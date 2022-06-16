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

 Date: 13/04/2022 16:38:43
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for imgroup
-- ----------------------------
DROP TABLE IF EXISTS `imgroup`;
CREATE TABLE `imgroup`  (
  `Id` varchar(36) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '组Id',
  `Name` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '组名称',
  `CreateTime` datetime(0) NULL DEFAULT NULL COMMENT '创建时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci COMMENT = 'AC_科室信息' ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
