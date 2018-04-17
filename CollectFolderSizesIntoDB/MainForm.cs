using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using System.Configuration;
using static CollectFolderSizesIntoDB.RelayAccountOnlineDataSet;

namespace CollectFolderSizesIntoDB
{
    public partial class MainForm : Form
    {
        private DateTime _now;
        private int secondsToRefresh = 10;
        private int _foldersCount = 0;

        public MainForm()
        {
            //msg.Visible = false;
            InitializeComponent();
        }

        private void CollectFolderSizesIntoDB_Click(object sender, EventArgs e)  //start processing
        {
            lblStatus.Text = "Processing....";
            Application.DoEvents();

            string rootFolder = txtRootFolder.Text;

            if (!Directory.Exists(rootFolder))
            {
                lblStatus.Text = " Directory does not exist";
                return;
            }

            FolderSizeDataTable datatable = new FolderSizeDataTable();
            _now = DateTime.Now.AddSeconds(secondsToRefresh);
            _foldersCount = 0;

            GetFolderInfo(rootFolder, ref datatable);   //for root folder

            AdapterUpdate(datatable);

            lblStatus.Text = "Done. Please check DBA.FolderSize database table";
        }


        private void GetFolderInfo(string folder, ref FolderSizeDataTable datatable)
        {
            _foldersCount++;
            try
            {
                Int64 fileSize = 0;
                int fileCount = 0;
                foreach (FileInfo fi in new DirectoryInfo(folder).EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    fileSize += fi.Length;
                    fileCount++;
                }

                datatable.AddFolderSizeRow(DateTime.Now, folder, fileCount, (int)(fileSize/1024));

                foreach (string childfolder in Directory.EnumerateDirectories(folder))    //for subfolders
                {
                    GetFolderInfo(childfolder, ref datatable);    //iterate
                }

                if (DateTime.Now > _now)
                {
                    _now = DateTime.Now.AddSeconds(secondsToRefresh);
                    AdapterUpdate(datatable);
                    datatable = new FolderSizeDataTable();
                    lblStatus.Text = "Processing.... " + string.Format("Done with {0} folders. Please wait..", _foldersCount);
                    Application.DoEvents();
                }

            }
            catch (Exception Ex)
            {
                lblStatus.Text = Ex.Message;
                Application.DoEvents();
            }

        }

        private void Delete_Preview_QA_Folders(string rootFolder, ref FolderSizeDataTable datatable)
        {
            try
            {
                foreach (string clientfolder in Directory.EnumerateDirectories(rootFolder))    //for client level folders
                {
                    foreach (string environmentFolder in Directory.EnumerateDirectories(clientfolder))    //for environment folders
                    {
                        if (environmentFolder.EndsWith("Preview") || environmentFolder.EndsWith("QA"))
                        {
                            Directory.Delete(environmentFolder, true);
                            datatable.AddFolderSizeRow(DateTime.Now, environmentFolder, 0, 0);
                            _foldersCount++;

                            if (DateTime.Now > _now)
                            {
                                _now = DateTime.Now.AddSeconds(secondsToRefresh);
                                AdapterUpdate(datatable);
                                datatable = new FolderSizeDataTable();
                                lblStatus.Text = "Processing.... " + string.Format("Done with {0} folders. Please wait..", _foldersCount);
                                Application.DoEvents();
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                lblStatus.Text = Ex.Message;
                Application.DoEvents();
            }

        }
        private void AdapterUpdate(FolderSizeDataTable dt)
        {
            DataTable dataTable = dt;

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["PatientCompassConnectionString"].ConnectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter();

                // Set the INSERT command and parameter.
                adapter.InsertCommand = new SqlCommand(
                    "INSERT INTO [DBA].[FolderSize] (GeneratedAt, FullFolderPath,NumberOfFiles,SizeInKBs) VALUES (@GeneratedAt, @FullFolderPath,@NumberOfFiles,@SizeInKBs);", connection);

                adapter.InsertCommand.Parameters.Add("@GeneratedAt", SqlDbType.DateTime, 50, "GeneratedAt");
                adapter.InsertCommand.Parameters.Add("@FullFolderPath", SqlDbType.NVarChar, 1000, "FullFolderPath");
                adapter.InsertCommand.Parameters.Add("@NumberOfFiles", SqlDbType.Int, 4, "NumberOfFiles");
                adapter.InsertCommand.Parameters.Add("@SizeInKBs", SqlDbType.Int, 4, "SizeInKBs");

                adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;

                // Set the batch size.
                adapter.UpdateBatchSize = 100000;

                try {
                    adapter.Update(dataTable);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Delete_Preview_QA_Folders_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Processing....";
            Application.DoEvents();

            string rootFolder = txtRootFolder.Text;

            if (!Directory.Exists(rootFolder))
            {
                lblStatus.Text = " Directory does not exist";
                return;
            }

            FolderSizeDataTable datatable = new FolderSizeDataTable();
            _now = DateTime.Now.AddSeconds(secondsToRefresh);
            _foldersCount = 0;

            Delete_Preview_QA_Folders(rootFolder, ref datatable);   //for root folder

            AdapterUpdate(datatable);

            lblStatus.Text = "Done. Please check DBA.FolderSize database table";
        }
    }
}
