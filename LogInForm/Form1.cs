using LiteDB;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace LogInForm
{
    public partial class Form1 : Form
    {
        private const string DatabasePath = "users.db";
        // Database file path - this is where LiteDB stores all data persistently

        public Form1()
        {
            InitializeComponent();
            // Initialize the database when the form loads
            InitializeDatabase();
            
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
                    using (Aes myAes = Aes.Create())
                    { //create a new AES object to generate a key and IV for encryption
                        byte[] password = EncryptStringToBytes_Aes("password", myAes.Key, myAes.IV);

                        users.Insert(new User { Username = "admin", Password = password });
                    }
                }
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
                txtPassword.Text = "";
                txtUsername.Text = "";//clear the fields when done

                // In a real app, you would open the main application form here
            }
            else
            {
                MessageBox.Show("Invalid credentials.");
            }
            
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
                using (Aes myAes = Aes.Create())
                { //create a new AES object to generate a key and IV for encryption
                    byte[] hashPassword = EncryptStringToBytes_Aes(password, myAes.Key, myAes.IV);
                    return users.Exists(u => u.Username == username && u.Password == hashPassword);
                }
            }
            // Connection automatically closes here
        }
        /// <summary>
        /// Handles the register button click event
        /// Creates a new user account in the database
        /// </summary>
        private void btnRegister_Click(object sender, EventArgs e)
        {
            // Get user input from textboxes. In production THESE SHOULD BE VALIDATED
            //null checked and rule checked
            string username = txtUsername.Text;
            string tempPassword = txtPassword.Text;
            using (Aes myAes = Aes.Create())
            { //create a new AES object to generate a key and IV for encryption
                byte[] password = EncryptStringToBytes_Aes(tempPassword, myAes.Key, myAes.IV);

                // Attempt to register the new user
                if (RegisterUser(username, password))
                {
                    MessageBox.Show("Registration successful! ");
                    txtPassword.Text = "";
                    txtUsername.Text = "";//clear the fields when done
                }
                else
                {
                    MessageBox.Show("Registration failed. Username may already exist.");
                }
            }
            
        }
        /// <summary>
        /// Registers a new user by inserting into the database
        /// Returns true if registration was successful
        /// </summary>
        private bool RegisterUser(string username, byte[] password)
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
        /// https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-9.0
        /// </summary>
        /// encrytion routin from MS
        
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                    }

                    encrypted = msEncrypt.ToArray();
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
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
        public byte[] Password { get; set; }//set to byte array for encryption.
    }
}
