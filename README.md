# FinSense - Personal Finance Dashboard
A full-stack web application built from the ground up with C# and the .NET ecosystem, designed to provide users with a comprehensive and intuitive platform to manage their personal finances, track accounts, and gain valuable insights into their spending habits.

This project demonstrates a strong command of backend development with ASP.NET Core, secure API integration, and modern frontend practices. It is an actively developing application with a roadmap for incorporating data visualizations and machine learning features.


# Features
## Currently Implemented
* Secure User Authentication: Full user registration and login system built with ASP.NET Core Identity, ensuring all financial data is private and secure.
* Plaid API Integration: Securely connect to real-world bank accounts using the Plaid API. Implements the complete, secure token exchange flow (link_token, public_token, access_token).
* Automated Account Aggregation: Automatically fetches and displays financial accounts (checking, savings, credit cards) associated with a user's linked institution.
* Transaction Synchronization: Robust logic to perform incremental syncs of transaction data, pulling new transactions since the last update and preventing duplicates.
* Interactive Dashboard UI: A clean, responsive user interface built with ASP.NET Core MVC/Razor Pages, HTML, and CSS.
* Data-Driven Visualizations: Initial dashboard includes a dynamic doughnut chart, rendered with Chart.js, that visualizes user spending by category.
* AI Powered Transaction Categorization: Engineered a predictive machine learning model with ML.NET that learns from a user's manual transaction corrections. The model is trained on user specific data and automatically predicts categories for newly synced transactions.


## In Progress / Roadmap
* Expanded Dashboard Widgets: Adding more visualizations, including a historical balance line chart and summary cards for key metrics.
* Manual Transaction Management: Allowing users to manually add, edit, and categorize transactions.
* Budgeting Tools: Implementing features to set monthly budgets by category and track progress.
* AI Powered Insights (ML.NET) for forecasting future spending patterns.
* Investment Portfolio Tracking: Integrating with Plaid's Investments product to monitor holdings and performance.

# Tech Stack
## Backend
* Framework: C#, .NET 8, ASP.NET Core MVC
* Database: PostgreSQL
* ORM: Entity Framework Core (using a code-first approach with migrations)
* Authentication: ASP.NET Core Identity
* API Client: Official Plaid .NET Client Library

## Frontend
* Templating: Razor Pages
* Styling: HTML5, CSS3, Bootstrap
* JavaScript: Vanilla JavaScript, Chart.js for data visualization

## Database Management
* PostgreSQL

# Getting Started

## Prerequisites
* .NET 8 SDK
* PostgreSQL
* An IDE like Visual Studio 2022 or VS Code
* Plaid API Keys

## Installation

```bash
#Clone the repository:
git clone https://github.com/your-username/your-repo-name.git

#Configure Secrets:
Navigate to the project directory in your terminal.

#Initialize user secrets for the project:
dotnet user-secrets init

#Add your Plaid API keys and database connection string to the secrets store:
dotnet user-secrets set "Plaid:ClientId" "your_client_id"
dotnet user-secrets set "Plaid:Secret" "your_sandbox_secret"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your_postgresql_connection_string"
```

### Set up the Database:
* Ensure your PostgreSQL server is running.
* Open the Package Manager Console in Visual Studio or use the .NET CLI.


```bash
#Apply the Entity Framework migrations to create the database schema:
dotnet ef database update
```

### Run the Application:
```bash
#Open the project in Visual Studio and press F5 or run the following command from your terminal:
dotnet run
```