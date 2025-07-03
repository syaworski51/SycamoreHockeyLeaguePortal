# Sycamore Hockey League
The Sycamore Hockey League was founded in 2021 as a fictional hockey league where all the games are played in the latest edition of the EA NHL Series on Xbox One (NHL 24 as of publication). Initially consisting of 12 NHL teams, the league doubled in size to 24 NHL teams in 2022 and introduced a promotion/relegation system with 8 NHL teams in the bottom league, nicknamed the “Dungeon League”.
Complete with schedules, standings, playoff brackets, and a wall of champions, this website combines my passion for software development and my love of hockey into one project.

## Objective
I started this project in January 2024 to streamline the processes of updating the scores, standings, playoff brackets, the wall of champions and many more operations. These processes were previously done using separate Google Sheets for the main schedule, team schedules and standings, which was error-prone and inefficient, even with spreadsheet formulas with references to cells in other spreadsheets. This project replaces those processes with a more secure, centralized and scalable web application that is more efficient and less error-prone.

## Technologies In Use
- ASP.NET MVC (C#)
  - Core framework of this project
  - Routing, controller logic, data retrieval, server-side rendering, role-based access control
- Azure SQL database with Managed Identity
  - Handles identity data (user accounts, roles, permissions, etc.) and production data (schedules, standings, seasons, etc.)
- REST API
  - Handles scorekeeping functionality for each game
- Bootstrap
  - Used for front-end design and UI layouts
- JavaScript
  - Used for front-end scripting, such as updating scoreboards on the front end

## Functionality
### Scorekeeping
- Authorized users can update the scores as appropriate via a custom scoreboard controller interface
- Scores are then updated on the backend via a dedicated REST API
- Standings are updated automatically as soon as a game is finalized
- Also contains logic for enabling/disabling buttons based on the game’s progress

### CSV Uploads for New Schedules
- Schedules are prepared in a Google spreadsheet, then adapted to the specified columns in CSV format and uploaded when ready
- Once uploaded, each game is automatically inserted into the database

### Role-Based Access Control
- Used for controlling access to sensitive league operations, such as updating scores, uploading schedules, etc.

### Gameplay
- As mentioned, all games are played in the latest edition of the EA NHL series on Xbox One (currently NHL 24) with coin flips to pick my team for each game.

## Plans for Future Improvements
- Synchronization service between local and live databases
  - Writes new data from the local database (source of truth) to the live database whenever the local database is updated (i.e.: scores become finalized, playoff matchups are updated, a new champion is crowned, etc.).
- Automated process for detecting potential clinching/elimination scenarios
  - Looks at the standings after an update for scenarios where a team could clinch a playoff spot, be eliminated from playoff contention, or even demoted to the bottom league.
- Automated process for updating playoff matchup information, such as the teams in the matchup
  - Current process is done manually. A future process may involve adding predecessor indexes to each matchup pointing to the matchups the teams will be coming from.
- Database triggers on certain tables to prevent unwanted changes
  - Examples of unwanted changes include re-entering test mode in a certain season, or editing a finalized score after a grace period for correcting any errors has expired.
