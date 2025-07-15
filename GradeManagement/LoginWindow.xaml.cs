using GradeManagement.DAL.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GradeManagement
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        GradeManagementSystemContext _context;
        public LoginWindow()
        {
            InitializeComponent();
            _context = new GradeManagementSystemContext();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = pbPassword.Password;

            if (username.IsNullOrEmpty() || password.IsNullOrEmpty())
            {
                MessageBox.Show("Nhập đầy đủ thông tin");
                return;
            }
            var userAccount = _context.UserAccounts
                .FirstOrDefault(u => u.Email == username && u.PasswordHash == password);
            if (userAccount != null)
            {
                if (userAccount.Role.Equals("khaothi"))
                {
                    StudentWindow studentWindow = new StudentWindow();
                    studentWindow.Show();
                    this.Close();
                }
                else if (userAccount.Role.Equals("lecturer"))
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                } 
                        
            }
            else
            {
                MessageBox.Show("Sai tk hoac mk"); return;
            }
        }
    }
}
