using GradeManagement.DAL;
using GradeManagement.DAL.Models;
using GradeManagement.DAL.Services;
using GradeManagement.DAL.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for StudentWindow.xaml
    /// </summary>
    public partial class StudentWindow : Window
    {
        StudentServices? _studentServices;
        int cnt;
        InsertStudentWindow? InsertStudentWindow;
        public StudentWindow()
        {
            InitializeComponent();
            _studentServices = new StudentServices();
            
            cnt = 0;


        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Begin();
            
        }

        public void Begin()
        {

            var students = _studentServices.GetStudentsWithCourses();
            dgStudents.ItemsSource = students;
           

        }

        private void dgStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStudents.SelectedItem is StudentViewModel selectedStudent)
            {
                btNewStudent.Content = "Update Student";
            }
            else
            {
                btNewStudent.Content = "Add New Student";
            }
        }
        
        private void Button_Click_Addnew(object sender, RoutedEventArgs e)
        {
            if (btNewStudent.Content.ToString() == "ADD NEW STUDENT")
            {
                if (cnt == 0 || !InsertStudentWindow.IsVisible)
                {
                    InsertStudentWindow = new InsertStudentWindow(this, null);
                    InsertStudentWindow.Show();
                    cnt++;
                }
                else
                {
                    InsertStudentWindow.Focus();
                }
            }
            else if (btNewStudent.Content.ToString() == "Update Student")
            {
                if (cnt == 0 || !InsertStudentWindow.IsVisible)
                {
                    InsertStudentWindow = new InsertStudentWindow(this, dgStudents.SelectedItem as StudentViewModel);
                    InsertStudentWindow.Show();
                    cnt++;
                }
                else
                {
                    InsertStudentWindow.Focus();
                }
               
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            String searchText = txtSearch.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgStudents.ItemsSource = _studentServices.GetStudentsWithCourses();
            }
            else
            {
                var filteredStudents = _studentServices.GetStudentsWithCourses()
                    .Where(s => s.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                s.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                s.RollNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                dgStudents.ItemsSource = filteredStudents;
            }
        }

        private void btnRefesh_Click(object sender, RoutedEventArgs e)
        {
            Refesh();
        }

        public void Refesh()
        {
            txtSearch.Text = string.Empty;
            dgStudents.ItemsSource = _studentServices.GetStudentsWithCourses();
            dgStudents.SelectedItem = null;
            btNewStudent.Content = "ADD NEW STUDENT";
        }

        private void Bt_Grade_Click(object sender, RoutedEventArgs e)
        {
            GradeWindow gradeWindow = new GradeWindow();
            gradeWindow.Show();
            this.Close();
        }
    }
}

