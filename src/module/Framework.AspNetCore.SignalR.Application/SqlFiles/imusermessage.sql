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

 Date: 13/04/2022 16:41:22
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for imusermessage
-- ----------------------------
DROP TABLE IF EXISTS `imusermessage`;
CREATE TABLE `imusermessage`  (
  `Id` varchar(36) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '消息id',
  `SenderId` varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '发送者id',
  `ReceiverId` varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '创建时间',
  `Content` varchar(2000) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '消息内容',
  `CreateTime` datetime(0) NULL DEFAULT NULL COMMENT '创建时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci COMMENT = 'AC_科室信息' ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
