using LiteDB;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Windows.Forms;

namespace LogInForm
{
    public partial class Form1 : Form
    {
        private const string DatabasePath = "C:\\Users\\771666\\OneDrive - wqeic.ac.uk\\users.db";
        // Database file path - this is where LiteDB stores all data persistently

        public Form1()
        {
            InitializeComponent();
            // Initialize the database when the form loads
            InitializeDatabase();
            // Update the UI to show current database status
            UpdateDatabaseInfo();
        }
        /// <summary>
        /// Initializes the database and creates sample data if needed
        /// This runs once when the application starts
        /// </summary>
        private void InitializeDatabase()
        {
            // The 'using' statement ensures the database connection is properly closed/disposed
            using (var db = new LiteDatabase(DatabasePath))
            {
                // Get or create the "users" collection (similar to a table in SQL)
                var users = db.GetCollection<User>("users");
                // Create an index on the Username field for fast lookups
                // The 'true' parameter makes the username unique (no duplicates allowed)
                users.EnsureIndex(u => u.Username, true);

                // Check if the database is empty (no users exist yet)
                if (users.Count() == 0)
                {
                    // Insert a default admin user for first-time use
                    // Note: In production, always hash passwords - this is just for demo
                    users.Insert(new User { Username = "admin", Password = "password" });
                }
            }
        }
        /// <summary>
        /// Updates the UI to show current database file information
        /// Demonstrates that data is stored persistently in a file
        /// </summary>
        private void UpdateDatabaseInfo()
        {
            // Check if the database file exists on disc
            if (File.Exists(DatabasePath))
            {
                // Get information about the physical database file
                var fileInfo = new FileInfo(DatabasePath);

                // Display file path and size to prove data is stored persistently
                lblStatus.Text = $"Database: {DatabasePath} ({fileInfo.Length} bytes)";
            }
            else
            {
                lblStatus.Text = "Database: Not found";
            }
        }
        /// <summary>
        /// Handles the login button click event
        /// Validates user credentials against the database
        /// </summary>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            // Validate credentials against the database
            if (ValidateUser(username, password))
            {
                MessageBox.Show("Login successful! Data persists between sessions.");
                // In a real app, you would open the main application form here
            }
            else
            {
                MessageBox.Show("Invalid credentials.");
            }
            // Refresh the database info display
            UpdateDatabaseInfo();
        }
        /// <summary>
        /// Validates user credentials by querying the database
        /// Returns true if username/password combination exists
        /// </summary>
        private bool ValidateUser(string username, string password)
        {
            // Open a new database connection for this query
            using (var db = new LiteDatabase(DatabasePath))
            {
                // Get the users collection
                var users = db.GetCollection<User>("users");

                // Query the database: check if a user exists with matching username AND password
                // The Exists() method returns true if any document matches the condition
                return users.Exists(u => u.Username == username && u.Password == password);
            }
            // Connection automatically closes here
        }
        /// <summary>
        /// Handles the register button click event
        /// Creates a new user account in the database
        /// </summary>
        private void btnRegister_Click(object sender, EventArgs e)
        {
            // Get user input from textboxes
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            // Attempt to register the new user
            if (RegisterUser(username, password))
            {
                MessageBox.Show("Registration successful! This user will persist after app closes.");
            }
            else
            {
                MessageBox.Show("Registration failed. Username may already exist.");
            }

            // Refresh the database info display
            UpdateDatabaseInfo();
        }
        /// <summary>
        /// Registers a new user by inserting into the database
        /// Returns true if registration was successful
        /// </summary>
        private bool RegisterUser(string username, string password)
        {
            try
            {
                // Open a new database connection
                using (var db = new LiteDatabase(DatabasePath))
                {
                    // Get the users collection
                    var users = db.GetCollection<User>("users");

                    // Check if username already exists (thanks to our unique index)
                    if (users.Exists(u => u.Username == username))
                        return false; // Registration failed - username taken

                    // Create and insert new user document
                    users.Insert(new User { Username = username, Password = password });
                    return true; // Registration successful
                }
            }
            catch
            {
                // Handle any unexpected errors (like file access issues)
                // In production, you would log this error
                return false;
            }
        }
        /// <summary>
        /// Demonstrates database persistence by showing all stored users
        /// This proves data survives application restarts
        /// </summary>
        private void btnShowUsers_Click(object sender, EventArgs e)
        {
            // Open database connection
            using (var db = new LiteDatabase(DatabasePath))
            {
                // Get all users from the collection
                var users = db.GetCollection<User>("users");

                // Retrieve all documents from the users collection
                var allUsers = users.FindAll();

                // Build a string showing all current users
                string userList = "Current users in database:\n";
                foreach (var user in allUsers)
                {
                    userList += $"- {user.Username}\n";
                }

                // Display the list to prove data is persistently stored
                MessageBox.Show(userList);
            }
        }

    }
    /// <summary>
    /// User model class - represents a user document in the database
    /// LiteDB automatically maps this class to BSON documents
    /// </summary>
    public class User
    {
        // LiteDB automatically uses this as the primary key (_id field)
        // The Id property is automatically assigned when inserting
        public int Id { get; set; }

        // Username for login - must be unique
        public string Username { get; set; }

        // Password for login (in production, this should be hashed)
        public string Password { get; set; }
    }
}
