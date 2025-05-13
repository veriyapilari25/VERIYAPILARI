# Organizational Hierarchy Management System (OrgTree)

This is a full-stack web application for managing an organization's employee structure.
The system allows administrators to add, update, delete, undo-delete,
and explore employee details such as departments, skills, and managerial relationships.
It also includes various ways to visualize and query the employee hierarchy using core data structures
like trees, hash tables, and stacks.

## 🔧 Tech Stack

- **Backend:** ASP.NET Core Web API
- **Frontend:** HTML, JavaScript, CSS
- **Database:** Entity Framework Core with SQL Server
- **Authinication** JWT Authentication
- `DepartmentController.cs` — CRUD operations for departments.
- `AuthController.cs` — Handles user registration, login, and role checking.
- **Models**
  - `Department.cs`, `Employee.cs`, `User.cs` — Entity classes.
  - `LoginModel`, `RegisterModel` — DTOs for auth operations.
  - `EmployeeDto` — To handle subordinate recursions and easier data management.
- **Data**
  - `ApplicationDBContext.cs` — EF Core DbContext for database access.
- **Html**
  - `index.html` — Represents the login page.
  - `register.html` — For adding new users.
  - `org-chart` — The main page where admin actions can take place, and you have two views of employees.
- **Data Structures Used:**
  - **Tree:** For visualizing and organizing employee hierarchy.
  - **Stack:** For undo delete functionality.
  - **Hash Table (Dictionary):** For quick lookup of employees by  name and skills.

## 📂 Features

### 🔐 Admin Features
- **Add New Employee**
- **Edit Employee Details**
- **Delete Employee (with validation that managers cannot be deleted)**
- **Undo Delete (restores the most recently deleted employee)**

### 🔍 Querying Features
- **Get Employee By ID**
- **Get All Employees**
- **Get All Employees with Subordinates**
- **Get Email by Employee Name (Hash Table)**
- **Get Employees by Skill (Hash Table)**
- **Get Department’s Employees**
- **Get All Managers**
- **Get All Subordinates for a Manager**
- **Visualize Employee Hierarchy as a Tree**
- **Get Employees by Hierarchical Levels**
- **View Organizational Graph (Tree Structure)**

## 📊 Data Structures Explained

- **Tree:** 
  - Used to build and visualize the organizational structure.
  - Supports recursive fetching of subordinates.

- **Stack:** 
  - Used for undo delete operation.
  - Stores recently deleted employees.

- **Hash Table:**
  - Lookup employees by name and skill efficiently.
  - Allows partial match search for skills.

## 📸 Screenshots

![Main View](https://github.com/user-attachments/assets/6cc66abf-c682-4b0e-b4c5-e45a8c36728b)
![Skill Search & Admin Panel](https://github.com/user-attachments/assets/81d6e4e4-6e8e-46da-9a49-6f1d5af3e074)

## 🔁 API Endpoints

### Employee CRUD
- `POST /api/Values/PostEmployee`
- `PUT /api/Values/UpdateEmployee/{id}`
- `DELETE /api/Values/DeleteEmployee/{id}`
- `POST /api/Values/UndoDeleteEmployee`

### Department CRUD
- `POST /api/Department`
- `PUT /api/Department/{id}`
- `DELETE /api/Department/{id}`

### Fetching Data
- `GET /api/Values/GetEmployees`
- `GET /api/Values/GetEmployeeByID/{id}`
- `GET /api/Values/GetAllEmployeesWithSubordinates`
- `GET /api/Values/GetEmailByName?name={name}`
- `GET /api/Values/GetDepartmentsEmployees/{id}`
- `GET /api/Values/GetEmployeesBySkill?skill={skill}`
- `GET /api/Values/GetAllManagers`
- `GET /api/Values/GetAllSubordinates/{managerId}`
- `GET /api/Values/EmployeeGraph`
- `GET /api/Values/EmployeeTree`
- `GET /api/Values/GetEmployeesByLevel`
- `GET /api/Values/GetUndoCount`
- `GET /api/Departments/GetDepartments`
- `GET /api/Values/GetUndoCount`
- `GET /api/Values/GetUndoCount`
- `GET /api/Values/`
- `GET /api/Department`
- `GET /api/Department/{id}`
- `GET /api/Department/{id}/Employees`

### Departments api hasn't been used in the project. You can add it by choice.

### ✅ Security Notes
- **Passwords are stored as plain text in this demo. You must use hashing (e.g., BCrypt) in production.**
- **JWT tokens are signed using HMAC SHA256. Store your key securely (use secrets manager or environment variables).**
- **The connection string should also be changed in the appsettings section.**


## 🚀 How to Run

### Prerequisites

**FrameWorks**
- Microsoft.AspNetCore.App
- Microsoft.NETCore.App
  
**Packages**
- Bissaye.JwtAuth (1.0.1)
- Microsoft.AspNetCore.Authentication.JwtBearer (9.0.4)
- Microsoft.AspNetCore.OpenApi (9.0.3)
- Microsoft.EntityFrameworkCore.SqlServer (9.0.3)
- Microsoft.EntityFrameworkCore.Tools (9.0.3)
- Scalar.AspNetCore (2.1.4)
- Swashbuckle.AspNetCore (8.1.0)
  
### 1. Clone the Repository

    git clone [https://github.com/yourusername/your-repo-name.git](https://github.com/veriyapilari25/VERIYAPILARI.git)
    cd VERIYAPILARI.git

### 2. Set up the Backend 

    dotnet restore
    dotnet ef database update
    dotnet run

### 3. Run the Frontend

    Open the `index.html` file in your browser.




