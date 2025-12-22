# 📝 TaskManager - ASP.NET Core MVC

A robust, full-stack Task Management application built with **ASP.NET Core MVC**. This project moves away from static page reloads to a dynamic, single-page-like experience, demonstrating modern web architecture using Entity Framework Core, **Secure BCrypt Authentication**, and AJAX.

## 💼 Live Site:
https://taskmanagerteam.azurewebsites.net/

![Project Status](https://img.shields.io/badge/status-active-success)
![License](https://img.shields.io/badge/license-MIT-blue)
![Security](https://img.shields.io/badge/Security-BCrypt-green)

## ✨ New Features (v2.0)

* **🔐 Enterprise-Grade Security:** Replaced plain-text storage with **BCrypt hashing and salting** to secure user passwords against database breaches.
* **🎨 Visual Identity System:** Users are auto-assigned a unique, permanent color based on their email. This color syncs across the Dashboard, Charts, and Dropdowns.
* **🔔 Smart Notifications:** Real-time alert bell showing personal deadlines:
    * ⚠️ **Red:** Overdue tasks.
    * 📅 **Orange:** Due today.
    * ⏳ **Blue:** Due tomorrow.
* **👮 Admin Monitoring:** A special "Critical Attention" panel for Admins to track all overdue tasks across the organization.
* **🔽 Universal Dropdown:** Integrated **Select2** for a rich, searchable "Assign To" menu that renders consistent custom colors on Windows, Mac, and Mobile.
* **📊 Advanced Analytics:** Interactive Chart.js integration grouping tasks by unique user emails.

## 🛠️ Core Features

* **User Authentication:** Claims-based RBAC (Role-Based Access Control) with **BCrypt Password Hashing**.
* **Task CRUD:** Create, Read, Update, and Delete tasks without page reloads (AJAX).
* **Search & Filter:** Real-time search by Title or Description.
* **Responsive UI:** Clean interface built with Bootstrap 5.
* **Database:** SQLite integration via Entity Framework Core (Code-First).

## 🧰 Tech Stack

* **Framework:** ASP.NET Core MVC (.NET 8)
* **Language:** C#
* **Database:** SQLite
* **ORM:** Entity Framework Core
* **Security:** **BCrypt.Net-Next** (Hashing)
* **Frontend:** HTML5, CSS3, Bootstrap 5
* **Libraries:**
    * **jQuery** (AJAX interactions)
    * **Select2** (Rich Dropdowns)
    * **Chart.js** (Data Visualization)
    * **FontAwesome** (Icons)

## 🚀 Getting Started

Follow these instructions to run the project locally.

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 or later)
* VS Code, Cursor, or Visual Studio
* *Note: An active internet connection is required for CDNs (FontAwesome, Select2).*

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
    *The database (`TaskManager_v2.db`) will be automatically created.*

4.  **Access the Dashboard**
    Open your browser and navigate to the port shown in your terminal.

## 🔑 Default Credentials

**Note:** Due to the new Security Hashing, previous plain-text accounts (like `123`) will no longer work.

Please **Register a New Account** on the login screen to generate a secure hash and access the dashboard.

## 🤝 Contributing

Contributions are welcome! If you'd like to improve the UI or add new features (like Categories or Email Notifications), feel free to fork the repository.

1.  Fork the Project
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the Branch (`git push origin feature/AmazingFeature`)
5.  Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
