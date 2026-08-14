-- ChatRoom 数据库初始化脚本（可重复执行）
-- 用法: mysql -u root -p < init.sql

CREATE DATABASE IF NOT EXISTS chat_service
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE chat_service;

CREATE TABLE IF NOT EXISTS `user` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `password` VARCHAR(255) NOT NULL COMMENT 'bcrypt 密码哈希',
  `nickname` VARCHAR(64) NOT NULL COMMENT '昵称',
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_nickname` (`nickname`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户表';

