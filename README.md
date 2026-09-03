# RaceDay System

## Project Overview

RaceDay is a database-driven race event management system designed to manage race events, participants, organisers, enrolments, supporting documents and race results.

The system allows organisers to create and manage race events while participants can register for events and manage their enrolments. Administrators have access to manage and oversee the system.

## User Roles

### Administrator
The Administrator manages and oversees the RaceDay system and has access to system information and race results.

### Event Organiser
The Event Organiser can create and update race events and view participants who have enrolled in their events.

### Participant
The Participant can register for an account, log in, view available events, enrol in events and submit supporting documents.

## Database

The RaceDay database contains the following entities:

- ROLE
- USER
- EVENT
- ENROLMENT
- CATEGORY
- SUPPORTING DOCUMENT
- RACE RESULT

The database relationships are represented in the Entity Relationship Diagram included in the `Docs` folder.

The SQL database script contains the table definitions, primary keys, foreign keys, constraints and sample data required for the RaceDay database.

## API Endpoint Plan

The planned API provides functionality for authentication, event management, participant enrolments, supporting documents and race results.

The API Endpoint Plan includes:

- User registration
- User login
- Viewing events
- Creating events
- Updating events
- Event enrolments
- Viewing event enrolments
- Supporting document submission
- Viewing race results
- Publishing race results

The complete API Endpoint Plan is available in the `Docs` folder.

## Repository Structure

```text
RaceDay/
│
├── Docs/
│   ├── API_Endpoint_Plan.pdf
│   ├── ERD.png
│   └── RaceDay.sql
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
└── README.md

<img width="1166" height="236" alt="image" src="https://github.com/user-attachments/assets/bc219776-901f-4ac4-988e-fbfe3059c96e" />

