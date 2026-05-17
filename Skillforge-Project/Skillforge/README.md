
🚀 Getting Started
Follow these steps to set up the environment and get the application running locally.

1. Environment Configuration
Create a .env file in the root directory of the project and define the following variables:

Code snippet
    JWT_SECRET_KEY=your_super_secret_key_here (should contains 50+ characters)
    CONNECTION_STRING=your connection String

2. Add all the Neccessary Packages using the command in bash
    dotnet restore 

3. Database Setup
The project uses Entity Framework Core. You will need to apply the migrations to initialize your local database schema.

Navigate to the Data/Domain layer:

Bash
cd Skillforge.Databases/Skillforge.Data
Generate and apply the migrations:

Bash
    dotnet ef migrations add MigrationName
    dotnet ef database update



4. Running the Application
Once the database is configured, return to the main project folder and start the server.

Return to the root:

Bash
cd ../..


Launch the project:
Bash
    dotnet run 