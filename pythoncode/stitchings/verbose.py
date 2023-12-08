import os

import cv2 as cv
import numpy as np
import copy

from .images import Images
from .seam_finder import SeamFinder
from .timelapser import Timelapser

import json


def verbose_stitching(stitcher, images, feature_masks=[], verbose_dir=None):
    _dir = "." if verbose_dir is None else verbose_dir

    output_Array = []


    images = Images.of(
        images, stitcher.medium_megapix, stitcher.low_megapix, stitcher.final_megapix
    )

    # Resize Images
    imgs = list(images.resize(Images.Resolution.MEDIUM))

    # Find Features
    finder = stitcher.detector

    gray_imgs = list()
    gray_imgs.append(cv.cvtColor(imgs[0], cv.COLOR_BGR2GRAY))
    gray_imgs.append(cv.cvtColor(imgs[1], cv.COLOR_BGR2GRAY))

    features = stitcher.find_features(gray_imgs, feature_masks)
    for idx, img_features in enumerate(features):
        img_with_features = finder.draw_keypoints(imgs[idx], img_features)
        # write_verbose_result(_dir, f"01_features_img{idx+1}.jpg", img_with_features)

    # Match Features
    matcher = stitcher.matcher
    matches = matcher.match_features(features)

    # print("MatchesInfo.matche.queryIdx:",  matches[1].matches[0].queryIdx)
    # print("MatchesInfo.matche.trainIdx:",  matches[1].matches[0].trainIdx)
    # print("MatchesInfo.matche.distance:",  matches[1].matches[0].distance)
    # print("MatchesInfo.matche.imgIdx:",  matches[1].matches[0].imgIdx)
    # print("MatchesInfo.num_matches:",  len(matches[1].matches))
    # print("MatchesInfo.num_inliers:", matches[1].num_inliers)
    # print("MatchesInfo.confidence:", matches[1].confidence)
    # print("MatchesInfo1.H:", matches[1].H)
    # print("MatchesInfo.inliers_mask:", matches[1].inliers_mask)

    # Subset
    subsetter = stitcher.subsetter

    all_relevant_matches = list(
        matcher.draw_matches_matrix(
            imgs,
            features,
            matches,
            conf_thresh=subsetter.confidence_threshold,
            inliers=True,
            matchColor=(0, 255, 0),
        )
    )
    # for idx1, idx2, img in all_relevant_matches:
    #     write_verbose_result(_dir, f"02_matches_img{idx1+1}_to_img{idx2+1}.jpg", img)

    # # Create MatchesInfo object
    matches_info = cv.detail_MatchesInfo()

    good_matches = []
    pair_matches1 = []
    pair_matches2 = []
    BFmatches = []
    tempMatches = []

    des1 = features[0].descriptors.get()
    des2 = features[1].descriptors.get()

    des1 = des1.astype('float32')
    des2 = des2.astype('float32')

    # Find 1->2 matches
    matcher = cv.FlannBasedMatcher()
    BFmatches = matcher.knnMatch(des1 , des2  , 2)

    # Match the descriptors using a BFMatcher object
    # bf = cv.BFMatcher(cv.NORM_HAMMING, crossCheck=True)
    # BFmatches = bf.match(des1, des2)

    # Sort the matches by their distance
    # BFmatches = sorted(BFmatches, key=lambda x: x.distance)
    # img_matches = cv.drawMatches(imgs[0], features[0].keypoints,imgs[1], features[1].keypoints, BFmatches, None, flags=cv.DrawMatchesFlags_NOT_DRAW_SINGLE_POINTS)

    # Display the image with matches

    for pair_match in BFmatches:
        if len(pair_match) < 2:
            continue
        m0 = pair_match[0]
        m1 = pair_match[1]
        if m0.distance < 0.7 * m1.distance:
            dmatch = cv.DMatch()
            dmatch.queryIdx = m0.queryIdx
            dmatch.trainIdx = m0.trainIdx
            dmatch.distance = m0.distance
            dmatch.imgIdx = 0
            pair_matches1.append(dmatch)
            good_matches . append([m0])
            dmatch1 = cv.DMatch()
            dmatch1.queryIdx = m0.trainIdx
            dmatch1.trainIdx = m0.queryIdx
            dmatch1.distance = m0.distance
            dmatch1.imgIdx = 0
            pair_matches2.append(dmatch1)

            tempMatches.append((m0.queryIdx, m0.trainIdx))


    img_matches = cv.drawMatchesKnn(imgs[0], features[0].keypoints,imgs[1], features[1].keypoints, good_matches, None, flags=2)
    # write_verbose_result(_dir, f"03_matches_img1_to_img2.jpg", img_matches)



    matches_info.matches = pair_matches1


    src_points = np.zeros((1, len(matches_info.matches), 2), dtype=np.float32)
    dst_points = np.zeros((1, len(matches_info.matches), 2), dtype=np.float32)

    for i, m in enumerate(matches_info.matches):
        p = features[0].keypoints[m.queryIdx].pt
        p = (p[0] - features[0].img_size[0] * 0.5, p[1] - features[0].img_size[1] * 0.5)
        src_points[0, i] = p

        p = features[1].keypoints[m.trainIdx].pt
        p = (p[0] - features[1].img_size[0] * 0.5, p[1] - features[1].img_size[1] * 0.5)
        dst_points[0, i] = p

    homographyResult = cv.findHomography(src_points, dst_points, cv.RANSAC)

    matches_info.H = homographyResult[0]
    matches_info.inliers_mask = np.ravel(homographyResult[1])

    matches_info.num_inliers = 0
    for inlier_mask in matches_info.inliers_mask:
        if inlier_mask:
            matches_info.num_inliers += 1

    matches_info.confidence = matches_info.num_inliers / len(matches_info.matches)


    # Construct point-point correspondences for inliers only
    src_points = np.zeros((1, matches_info.num_inliers, 2), dtype=np.float32)
    dst_points = np.zeros((1, matches_info.num_inliers, 2), dtype=np.float32)
    inlier_idx = 0
    for i in range(len(matches_info.matches)):
        if not matches_info.inliers_mask[i]:
            continue

        m = matches_info.matches[i]

        p = features[0].keypoints[m.queryIdx].pt
        p = (p[0] - features[0].img_size[0] * 0.5, p[1] - features[0].img_size[1] * 0.5)
        src_points[0, inlier_idx] = p

        p = features[1].keypoints[m.trainIdx].pt
        p = (p[0] - features[1].img_size[0] * 0.5, p[1] - features[1].img_size[1] * 0.5)
        dst_points[0, inlier_idx] = p

        inlier_idx += 1

    # Rerun motion estimation on inliers only
    matches_info.H, _ = cv.findHomography(src_points, dst_points, cv.RANSAC)

    matches_info.src_img_idx = 0
    matches_info.dst_img_idx = 1

    matches_info1 = cv.detail_MatchesInfo()
    matches_info1.confidence = matches_info.confidence
    matches_info1.matches = pair_matches2
    matches_info1.num_inliers = matches_info.num_inliers
    matches_info1.inliers_mask = matches_info.inliers_mask
    matches_info1.H = matches_info.H
    # matches_info1 = matches_info.copy()

    matches_info1.src_img_idx = 1
    matches_info1.dst_img_idx = 0


    print("MatchesInfo.H:", matches_info.H)

    if matches_info.H.size != 0:
        matches_info1.H = np.linalg.inv(matches_info1.H)
    print("MatchesInfo1.H:", matches_info1.H)



    matches_info1.matches = list(matches_info1.matches)  # Convert tuple to list

    for match in matches_info1.matches:
        match.queryIdx , match.trainIdx  = match.trainIdx , match.queryIdx

    matches_info1.matches = tuple(matches_info1.matches)  # Convert list back to tuple
    matches = list(matches)  # Convert tuple to list
    matches[1] = matches_info  # Modify the value
    matches[2] = matches_info1
    matches = tuple(matches)  # Convert list back to tuple

    matches_info = cv.detail_MatchesInfo()


    # Subset
    subsetter = stitcher.subsetter
    # subsetter.save_file = verbose_output(_dir, "03_matches_graph.txt")
    subsetter.save_matches_graph_dot_file(images.names, matches)

    indices = subsetter.get_indices_to_keep(features, matches)

    imgs = subsetter.subset_list(imgs, indices)
    features = subsetter.subset_list(features, indices)
    matches = subsetter.subset_matches(matches, indices)
    images.subset(indices)

    # Camera Estimation, Adjustion and Correction
    camera_estimator = stitcher.camera_estimator
    camera_adjuster = stitcher.camera_adjuster
    wave_corrector = stitcher.wave_corrector

    cameras = camera_estimator.estimate(features, matches)
    cameras = camera_adjuster.adjust(features, matches, cameras)
    cameras = wave_corrector.correct(cameras)

    # Warp Images
    low_imgs = list(images.resize(Images.Resolution.LOW, imgs))
    imgs = None  # free memory

    warper = stitcher.warper
    warper.set_scale(cameras)

    low_sizes = images.get_scaled_img_sizes(Images.Resolution.LOW)
    camera_aspect = images.get_ratio(Images.Resolution.MEDIUM, Images.Resolution.LOW)

    low_imgs = list(warper.warp_images(low_imgs, cameras, camera_aspect))
    low_masks = list(warper.create_and_warp_masks(low_sizes, cameras, camera_aspect))
    low_corners, low_sizes = warper.warp_rois(low_sizes, cameras, camera_aspect)

    final_sizes = images.get_scaled_img_sizes(Images.Resolution.FINAL)
    camera_aspect = images.get_ratio(Images.Resolution.MEDIUM, Images.Resolution.FINAL)

    final_imgs = list(images.resize(Images.Resolution.FINAL))

    output_Array.append(warper.scale * camera_aspect)

    for i in range(3):
        for j in range(3):
            output_Array.append(cameras[0].R[i][j])

    for i in range(3):
        for j in range(3):
            output_Array.append(cameras[1].R[i][j])

    Karray1 = get_K(cameras[0] , camera_aspect)
    Karray2 = get_K(cameras[1] , camera_aspect)

    for i in range(3):
        for j in range(3):
            output_Array.append(Karray1[i][j])

    for i in range(3):
        for j in range(3):
            output_Array.append(Karray2[i][j])

    final_imgs = list(warper.warp_images(final_imgs, cameras, camera_aspect))
    final_masks = list(
        warper.create_and_warp_masks(final_sizes, cameras, camera_aspect)
    )
    final_corners, final_sizes = warper.warp_rois(final_sizes, cameras, camera_aspect)

    # for idx, warped_img in enumerate(final_imgs):
        # write_verbose_result(_dir, f"04_warped_img{idx+1}.jpg", warped_img)

    # Excursion: Timelapser
    timelapser = Timelapser("as_is")
    timelapser.initialize(final_corners, final_sizes)

    for idx, (img, corner) in enumerate(zip(final_imgs, final_corners)):
        timelapser.process_frame(img, corner)
        frame = timelapser.get_frame()
        # write_verbose_result(_dir, f"05_timelapse_img{idx+1}.jpg", frame)

    # Crop
    cropper = stitcher.cropper

    if cropper.do_crop:
        mask = cropper.estimate_panorama_mask(
            low_imgs, low_masks, low_corners, low_sizes
        )
        # write_verbose_result(_dir, "06_estimated_mask_to_crop.jpg", mask)

        lir = cropper.estimate_largest_interior_rectangle(mask)

        # lir_to_crop = lir.draw_on(mask, size=2)
        # write_verbose_result(_dir, "06_lir.jpg", lir_to_crop)

        low_corners = cropper.get_zero_center_corners(low_corners)
        cropper.prepare(low_imgs, low_masks, low_corners, low_sizes)

        low_masks = list(cropper.crop_images(low_masks))
        low_imgs = list(cropper.crop_images(low_imgs))
        low_corners, low_sizes = cropper.crop_rois(low_corners, low_sizes)

        lir_aspect = images.get_ratio(Images.Resolution.LOW, Images.Resolution.FINAL)
        final_masks = list(cropper.crop_images(final_masks, lir_aspect))
        final_imgs = list(cropper.crop_images(final_imgs, lir_aspect))
        final_corners, final_sizes = cropper.crop_rois(
            final_corners, final_sizes, lir_aspect
        )

        output_Array.append(cropper.intersection_rectangles[0][0] * lir_aspect)
        output_Array.append(cropper.intersection_rectangles[0][1] * lir_aspect)
        output_Array.append(cropper.intersection_rectangles[0][2] * lir_aspect)
        output_Array.append(cropper.intersection_rectangles[0][3] * lir_aspect)

            
        output_Array.append(cropper.intersection_rectangles[1][0] * lir_aspect)
        output_Array.append(cropper.intersection_rectangles[1][1] * lir_aspect)
        output_Array.append(cropper.intersection_rectangles[1][2] * lir_aspect)
        output_Array.append(cropper.intersection_rectangles[1][3] * lir_aspect)

        timelapser = Timelapser("as_is")
        timelapser.initialize(final_corners, final_sizes)

        for idx, (img, corner) in enumerate(zip(final_imgs, final_corners)):
            timelapser.process_frame(img, corner)
            frame = timelapser.get_frame()
            # write_verbose_result(_dir, f"07_timelapse_cropped_img{idx+1}.jpg", frame)

    # Seam Masks
    seam_finder = stitcher.seam_finder

    seam_masks = seam_finder.find(final_imgs, final_corners, final_masks)
    seam_masks = [
        seam_finder.resize(seam_mask, mask)
        for seam_mask, mask in zip(seam_masks, final_masks)
    ]


    write_verbose_result(_dir , f"mask1.jpg" , seam_masks[0])
    write_verbose_result(_dir , f"mask2.jpg" , seam_masks[1])

    seam_masks_plots = [
        SeamFinder.draw_seam_mask(img, seam_mask)
        for img, seam_mask in zip(final_imgs, seam_masks)
    ]

    
    output_Array.append(final_corners[0][0])
    output_Array.append(final_corners[0][1])
    output_Array.append(final_corners[1][0])
    output_Array.append(final_corners[1][1])

    np.savetxt("data2.txt" , output_Array)

    # for idx, seam_mask in enumerate(seam_masks_plots):
    #     write_verbose_result(_dir, f"08_seam_mask{idx+1}.jpg", seam_mask)

    # Exposure Error Compensation
    compensator = stitcher.compensator

    compensator.feed(low_corners, low_imgs, low_masks)

    compensated_imgs = [
        compensator.apply(idx, corner, img, mask)
        for idx, (img, mask, corner) in enumerate(
            zip(final_imgs, final_masks, final_corners)
        )
    ]

    # for idx, compensated_img in enumerate(compensated_imgs):
        # write_verbose_result(_dir, f"08_compensated{idx+1}.jpg", compensated_img)

    # Blending
    blender = stitcher.blender
    blender.prepare(final_corners, final_sizes)
    for img, mask, corner in zip(compensated_imgs, seam_masks, final_corners):
        blender.feed(img, mask, corner)
    panorama, _ = blender.blend()

    # write_verbose_result(_dir, "09_result.jpg", panorama)

    # blended_seam_masks = seam_finder.blend_seam_masks(
    #     seam_masks, final_corners, final_sizes
    # )
    # with_seam_lines = seam_finder.draw_seam_lines(
    #     panorama, blended_seam_masks, linesize=3
    # )
    # with_seam_polygons = seam_finder.draw_seam_polygons(panorama, blended_seam_masks)

    # write_verbose_result(_dir, "09_result_with_seam_lines.jpg", with_seam_lines)
    # write_verbose_result(_dir, "09_result_with_seam_polygons.jpg", with_seam_polygons)

    return panorama


def write_verbose_result(dir_name, img_name, img):
    cv.imwrite(verbose_output(dir_name, img_name), img)


def verbose_output(dir_name, file):
    return os.path.join(dir_name, file)

def get_K(camera, aspect=1):
    K = camera.K().astype(np.float32)
    """ Modification of intrinsic parameters needed if cameras were
    obtained on different scale than the scale of the Images which should
    be warped """
    K[0, 0] *= aspect
    K[0, 2] *= aspect
    K[1, 1] *= aspect
    K[1, 2] *= aspect
    return K
