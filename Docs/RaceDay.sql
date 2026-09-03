CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- ============================================
-- ROLE TABLE
-- ============================================

CREATE TABLE ROLE
(
    role_id INT IDENTITY(1,1) PRIMARY KEY,
    role_name VARCHAR(30) NOT NULL UNIQUE
);
GO


-- ============================================
-- USER TABLE
-- ============================================

CREATE TABLE [USER]
(
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    role_id INT NOT NULL,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    profile_pic VARCHAR(255) NOT NULL DEFAULT 'default-profile.png',

    CONSTRAINT FK_USER_ROLE
        FOREIGN KEY (role_id)
        REFERENCES ROLE(role_id)
);
GO


-- ============================================
-- EVENT TABLE
-- ============================================

CREATE TABLE EVENT
(
    event_id INT IDENTITY(1,1) PRIMARY KEY,
    organiser INT NOT NULL,
    event_name VARCHAR(100) NOT NULL,
    description VARCHAR(500) NOT NULL,
    event_date DATE NOT NULL,
    location VARCHAR(150) NOT NULL,
    distance DECIMAL(5,2) NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    banner_image VARCHAR(255) NOT NULL DEFAULT 'default-banner.jpg',

    CONSTRAINT FK_EVENT_ORGANISER
        FOREIGN KEY (organiser)
        REFERENCES [USER](user_id),

    CONSTRAINT CK_EVENT_DISTANCE
        CHECK (distance > 0)
);
GO


-- ============================================
-- ENROLMENT TABLE
-- ============================================

CREATE TABLE ENROLMENT
(
    enr_id INT IDENTITY(1,1) PRIMARY KEY,
    participant INT NOT NULL,
    event_id INT NOT NULL,
    enr_date DATE NOT NULL DEFAULT GETDATE(),
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',

    CONSTRAINT FK_ENROLMENT_PARTICIPANT
        FOREIGN KEY (participant)
        REFERENCES [USER](user_id),

    CONSTRAINT FK_ENROLMENT_EVENT
        FOREIGN KEY (event_id)
        REFERENCES EVENT(event_id),

    CONSTRAINT CK_ENROLMENT_STATUS
        CHECK (status IN ('Pending', 'Confirmed', 'Cancelled'))
);
GO


-- ============================================
-- CATEGORY TABLE
-- ============================================

CREATE TABLE CATEGORY
(
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    event_id INT NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    description VARCHAR(300) NOT NULL,

    CONSTRAINT FK_CATEGORY_EVENT
        FOREIGN KEY (event_id)
        REFERENCES EVENT(event_id)
);
GO


-- ============================================
-- SUPPORTING DOCUMENT TABLE
-- ============================================

CREATE TABLE [SUPPORTING DOCUMENT]
(
    document_id INT IDENTITY(1,1) PRIMARY KEY,
    enr_id INT NOT NULL,
    file_name VARCHAR(150) NOT NULL,
    file_path VARCHAR(255) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',

    CONSTRAINT FK_DOCUMENT_ENROLMENT
        FOREIGN KEY (enr_id)
        REFERENCES ENROLMENT(enr_id),

    CONSTRAINT CK_DOCUMENT_STATUS
        CHECK (status IN ('Pending', 'Approved', 'Rejected'))
);
GO


-- ============================================
-- RACE RESULT TABLE
-- ============================================

CREATE TABLE [RACE RESULT]
(
    result_id INT IDENTITY(1,1) PRIMARY KEY,
    enr_id INT NOT NULL UNIQUE,
    finish_time TIME NOT NULL,
    position INT NOT NULL,
    published BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_RESULT_ENROLMENT
        FOREIGN KEY (enr_id)
        REFERENCES ENROLMENT(enr_id),

    CONSTRAINT CK_RESULT_POSITION
        CHECK (position > 0)
);
GO


-- ============================================
-- SAMPLE ROLE DATA
-- ============================================

INSERT INTO ROLE (role_name)
VALUES
('Admin'),
('Organiser'),
('Participant');
GO


-- ============================================
-- SAMPLE USER DATA
-- 2 Organisers
-- 2 Participants
-- 1 Admin
-- ============================================

INSERT INTO [USER]
    (role_id, first_name, last_name, email, password_hash, phone, profile_pic)
VALUES
    (2, 'Thabo', 'Mkhize', 'thabo.mkhize@example.com', 'hashed_password_001', '0821112233', 'thabo.jpg'),
    (2, 'Lerato', 'Naidoo', 'lerato.naidoo@example.com', 'hashed_password_002', '0832223344', 'lerato.jpg'),
    (3, 'Sipho', 'Dlamini', 'sipho.dlamini@example.com', 'hashed_password_003', '0843334455', 'sipho.jpg'),
    (3, 'Amahle', 'Khumalo', 'amahle.khumalo@example.com', 'hashed_password_004', '0854445566', 'amahle.jpg'),
    (1, 'Nomsa', 'Mthembu', 'nomsa.mthembu@example.com', 'hashed_password_005', '0865556677', 'nomsa.jpg');
GO


-- ============================================
-- SAMPLE EVENT DATA
-- 3 EVENTS
-- ============================================

INSERT INTO EVENT
    (organiser, event_name, description, event_date, location, distance, event_type, banner_image)
VALUES
    (1, 'Durban Sunrise Run',
     'A community road race along the Durban beachfront.',
     '2026-10-10',
     'Durban Beachfront',
     10.00,
     'Road Race',
     'durban-sunrise.jpg'),

    (1, 'Umhlanga Coastal Challenge',
     'A coastal running event for recreational and competitive runners.',
     '2026-11-07',
     'Umhlanga Rocks',
     15.00,
     'Coastal Run',
     'umhlanga-challenge.jpg'),

    (2, 'Pietermaritzburg Marathon',
     'A long distance road race through Pietermaritzburg.',
     '2026-12-05',
     'Pietermaritzburg',
     42.20,
     'Marathon',
     'pm-marathon.jpg');
GO


-- ============================================
-- SAMPLE CATEGORY DATA
-- CATEGORIES FOR EACH EVENT
-- ============================================

INSERT INTO CATEGORY
    (event_id, category_name, description)
VALUES
    (1, '10 km Open', 'Open category for runners participating in the 10 km race.'),
    (1, '10 km Junior', 'Junior category for younger participants in the 10 km race.'),
    (2, '15 km Open', 'Open category for runners participating in the 15 km coastal challenge.'),
    (2, '15 km Veteran', 'Veteran category for experienced runners.'),
    (3, 'Marathon Open', 'Open category for participants completing the full marathon.'),
    (3, 'Marathon Veteran', 'Veteran category for marathon participants.');
GO


-- ============================================
-- SAMPLE ENROLMENT DATA
-- ============================================

INSERT INTO ENROLMENT
    (participant, event_id, enr_date, status)
VALUES
    (3, 1, '2026-09-01', 'Confirmed'),
    (4, 1, '2026-09-02', 'Confirmed'),
    (3, 2, '2026-09-03', 'Confirmed'),
    (4, 2, '2026-09-03', 'Pending'),
    (3, 3, '2026-09-04', 'Confirmed'),
    (4, 3, '2026-09-04', 'Pending');
GO


-- ============================================
-- SAMPLE SUPPORTING DOCUMENT DATA
-- ============================================

INSERT INTO [SUPPORTING DOCUMENT]
    (enr_id, file_name, file_path, status)
VALUES
    (1, 'sipho-id.pdf', '/documents/sipho-id.pdf', 'Approved'),
    (2, 'amahle-id.pdf', '/documents/amahle-id.pdf', 'Approved'),
    (3, 'sipho-medical.pdf', '/documents/sipho-medical.pdf', 'Approved'),
    (4, 'amahle-medical.pdf', '/documents/amahle-medical.pdf', 'Pending');
GO


-- ============================================
-- SAMPLE RACE RESULT DATA
-- ============================================

INSERT INTO [RACE RESULT]
    (enr_id, finish_time, position, published)
VALUES
    (1, '00:52:31', 1, 1),
    (2, '00:58:44', 2, 1),
    (3, '01:18:22', 1, 1);
GO


-- ============================================
-- VERIFY THE DATA
-- ============================================

SELECT * FROM ROLE;
SELECT * FROM [USER];
SELECT * FROM EVENT;
SELECT * FROM CATEGORY;
SELECT * FROM ENROLMENT;
SELECT * FROM [SUPPORTING DOCUMENT];
SELECT * FROM [RACE RESULT];
GO