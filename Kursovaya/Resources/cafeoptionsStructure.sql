-- --------------------------------------------------------
-- Хост:                         127.0.0.1
-- Версия сервера:               8.0.30 - MySQL Community Server - GPL
-- Операционная система:         Win64
-- HeidiSQL Версия:              12.1.0.6537
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Дамп структуры базы данных CafeActivities
CREATE DATABASE IF NOT EXISTS `CafeActivities` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `CafeActivities`;

-- Дамп структуры для таблица CafeActivities.Categories
CREATE TABLE IF NOT EXISTS `Categories` (
  `IDcategory` int NOT NULL AUTO_INCREMENT,
  `Category` varchar(50) NOT NULL,
  PRIMARY KEY (`IDcategory`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Clients
CREATE TABLE IF NOT EXISTS `Clients` (
  `IDclient` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  `NumberPhone` varchar(16) NOT NULL,
  PRIMARY KEY (`IDclient`)
) ENGINE=InnoDB AUTO_INCREMENT=42 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Dishes
CREATE TABLE IF NOT EXISTS `Dishes` (
  `Article` varchar(6) NOT NULL,
  `IdEvent` int NOT NULL,
  `IdCategory` int NOT NULL,
  `Name` varchar(100) NOT NULL,
  `Compound` text NOT NULL,
  `Weight` int NOT NULL,
  `Price` int NOT NULL,
  `Photo` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`Article`),
  KEY `IdEvent` (`IdEvent`),
  KEY `IdCategory` (`IdCategory`),
  CONSTRAINT `dishes_ibfk_1` FOREIGN KEY (`IdEvent`) REFERENCES `Events` (`IDevent`),
  CONSTRAINT `dishes_ibfk_2` FOREIGN KEY (`IdCategory`) REFERENCES `Categories` (`IDcategory`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Events
CREATE TABLE IF NOT EXISTS `Events` (
  `IDevent` int NOT NULL AUTO_INCREMENT,
  `Event` varchar(50) NOT NULL,
  PRIMARY KEY (`IDevent`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.OrderComposition
CREATE TABLE IF NOT EXISTS `OrderComposition` (
  `IdOrder` int NOT NULL,
  `IdDish` varchar(6) NOT NULL,
  `Count` int NOT NULL,
  PRIMARY KEY (`IdOrder`,`IdDish`),
  KEY `IdDish` (`IdDish`),
  CONSTRAINT `ordercomposition_ibfk_1` FOREIGN KEY (`IdOrder`) REFERENCES `Orders` (`NumberOrder`),
  CONSTRAINT `ordercomposition_ibfk_2` FOREIGN KEY (`IdDish`) REFERENCES `Dishes` (`Article`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Orders
CREATE TABLE IF NOT EXISTS `Orders` (
  `NumberOrder` int NOT NULL AUTO_INCREMENT,
  `IdClient` int NOT NULL,
  `NumberPhoneClient` varchar(11) NOT NULL,
  `DateOfConclusion` date NOT NULL,
  `DateEvent` date NOT NULL,
  `IdSchedule` int NOT NULL,
  `IdStatus` int NOT NULL,
  `IdEvent` int NOT NULL,
  `IdUser` int NOT NULL,
  `Price` int NOT NULL,
  `DiscountAmount` int DEFAULT NULL,
  `PriceAll` int NOT NULL,
  `Prepayment` int DEFAULT NULL,
  PRIMARY KEY (`NumberOrder`),
  KEY `IdClient` (`IdClient`),
  KEY `IdSchedule` (`IdSchedule`),
  KEY `IdStatus` (`IdStatus`),
  KEY `IdEvent` (`IdEvent`),
  KEY `IdUser` (`IdUser`),
  CONSTRAINT `orders_ibfk_1` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IDclient`),
  CONSTRAINT `orders_ibfk_2` FOREIGN KEY (`IdSchedule`) REFERENCES `Schedule` (`IDschedule`),
  CONSTRAINT `orders_ibfk_3` FOREIGN KEY (`IdStatus`) REFERENCES `Status` (`IDstatus`),
  CONSTRAINT `orders_ibfk_4` FOREIGN KEY (`IdEvent`) REFERENCES `Events` (`IDevent`),
  CONSTRAINT `orders_ibfk_5` FOREIGN KEY (`IdUser`) REFERENCES `Users` (`IDuser`)
) ENGINE=InnoDB AUTO_INCREMENT=274 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Roles
CREATE TABLE IF NOT EXISTS `Roles` (
  `IDrole` int NOT NULL AUTO_INCREMENT,
  `Role` varchar(50) NOT NULL,
  PRIMARY KEY (`IDrole`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Schedule
CREATE TABLE IF NOT EXISTS `Schedule` (
  `IDschedule` int NOT NULL AUTO_INCREMENT,
  `StartTime` time NOT NULL,
  `EndTime` time NOT NULL,
  PRIMARY KEY (`IDschedule`)
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Status
CREATE TABLE IF NOT EXISTS `Status` (
  `IDstatus` int NOT NULL AUTO_INCREMENT,
  `Status` varchar(50) NOT NULL,
  PRIMARY KEY (`IDstatus`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица CafeActivities.Users
CREATE TABLE IF NOT EXISTS `Users` (
  `IDuser` int NOT NULL AUTO_INCREMENT,
  `FullName` varchar(100) NOT NULL,
  `Login` varchar(34) NOT NULL,
  `Password` varchar(64) NOT NULL,
  `IdRole` int NOT NULL,
  PRIMARY KEY (`IDuser`),
  KEY `IdRole` (`IdRole`),
  CONSTRAINT `users_ibfk_1` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IDrole`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Экспортируемые данные не выделены.

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
