# 📝 TaskManager - ASP.NET Core MVC

A full-stack Task Management application built with **ASP.NET Core MVC**. This project demonstrates modern web architecture using Entity Framework Core, Claims-based Authentication, and AJAX for a seamless user experience.

![Project Status](https://img.shields.io/badge/status-active-success)
![License](https://img.shields.io/badge/license-MIT-blue)

## ✨ Features

* **User Authentication:** Secure Login/Register system using Claims & Cookies.
* **Task CRUD:** Create, Read, Update, and Delete tasks without page reloads (AJAX).
* **Assignment System:** Assign tasks to specific users dynamically.
* **Search & Filter:** Real-time search by Title or Description.
* **Responsive UI:** Clean interface built with Bootstrap 5.
* **Database:** SQLite integration via Entity Framework Core (Code-First).

## 🛠️ Tech Stack

* **Framework:** ASP.NET Core MVC (.NET 8/10)
* **Language:** C#
* **Database:** SQLite
* **ORM:** Entity Framework Core
* **Frontend:** HTML5, CSS3, Bootstrap 5
* **Scripting:** jQuery (AJAX)

## 🚀 Getting Started

Follow these instructions to run the project locally.

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/download) (Version 6.0 or later)
* VS Code, Cursor, or Visual Studio

### Installation

1.  **Clone the repository**
    ```bash
    git clone [https://github.com/yourusername/taskmanager.git](https://github.com/yourusername/taskmanager.git)
    cd taskmanager
    ```

2.  **Restore Dependencies**
    ```bash
    dotnet restore
    ```

3.  **Run the Application**
    ```bash
    dotnet run
    ```
    *The database (`TaskManager.db`) will be automatically created and seeded with a default Admin user upon the first run.*

4.  **Access the Dashboard**
    Open your browser and navigate to: `http://localhost:5082` (or the port shown in your terminal).

## 🔑 Default Credentials

Use these credentials to log in for the first time:

* **Email:** `admin@domain.com`
* **Password:** `123`
*(Or use the Sign-Up link to create a new user)*

## 📂 Project Structure

TaskManager/
│
├── Controllers/
│   ├── TasksController.cs
│   └── AccountController.cs
│
├── Models/
│   ├── User.cs
│   ├── TaskItem.cs
│   └── AppDbContext.cs
│
├── Views/
│   ├── Tasks/
│   └── Account/
│
├── wwwroot/
│   └── js/
│       └── taskManager.js
│
├── Program.cs
└── appsettings.json

## 🤝 Contributing

Contributions are welcome! If you'd like to improve the UI or add new features (like Categories or Email Notifications), feel free to fork the repository.

1.  Fork the Project
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the Branch (`git push origin feature/AmazingFeature`)
5.  Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
