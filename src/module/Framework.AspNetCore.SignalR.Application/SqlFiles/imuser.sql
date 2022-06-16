/*
 Navicat Premium Data Transfer

 Source Server         : Tj_Aali_Server
 Source Server Type    : MySQL
 Source Server Version : 80011
 Source Host           : Tj_Aali_Server:3306
 Source Schema         : quzhou_baseasset

 Target Server Type    : MySQL
 Target Server Version : 80011
 File Encoding         : 65001

 Date: 13/04/2022 16:38:22
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for imuser
-- ----------------------------
DROP TABLE IF EXISTS `imuser`;
CREATE TABLE `imuser`  (
  `UserId` varchar(36) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '用户Id',
  `Confirmed` bit(1) NULL DEFAULT NULL COMMENT '是否需要确认消息',
  PRIMARY KEY (`UserId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci COMMENT = 'AC_科室信息' ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
