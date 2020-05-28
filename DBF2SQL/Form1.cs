using DbfDataReader;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBF2SQL
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        private bool ImportDeleted;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "DBF")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "DBF"));
            }
            if (File.Exists("sqldata.csv"))
            {
                var result = File.ReadAllText("sqldata.csv");
                try
                {
                    var split = result.Split(',');
                    textBox1.Text = split[0];
                    textBox2.Text = split[1];
                    textBox3.Text = split[2];
                    textBox5.Text = split[3];
                    textBox6.Text = split[4];
                }
                catch
                {

                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => { return ConvertData("N"); });
        }

        private Task<object> ConvertData(string yesno)
        {
            MySqlConnection con = null;
            string connectstring = "";
            int Lines = 20;
        Sql:
            if (yesno != "Y" && yesno != "N")
            {
                goto Sql;
            }
            else
            {
                if (yesno == "Y")
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        Console.Write("SQL Host: ");
                        string host = textBox1.Text;
                        Console.Write("SQL Port: ");
                        string port = textBox2.Text;
                        Console.Write("SQL Username: ");
                        string userid = textBox3.Text;
                        Console.Write("SQL Password: ");
                        string password = textBox5.Text;
                        Console.Write("SQL database: ");
                        string db = textBox6.Text;
                        connectstring = $"server={host};port={port};uid={userid};pwd={password};database={db}";
                        File.WriteAllText("sqldata.csv", textBox1.Text+"," + textBox2.Text + "," + textBox3.Text + "," + textBox5.Text + "," + textBox6.Text);
                        con = new MySqlConnection(connectstring);
                    });
                }
                this.Invoke((MethodInvoker)delegate
                {
                    Lines = Convert.ToInt32(numericUpDown1.Value);
                });
            }
            var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory,"DBF"), "*.dbf");
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "SQL")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "SQL"));
            }
            this.Invoke((MethodInvoker)delegate
            {
                progressBar1.Maximum = files.Length;
            });
            int filenum = 0;
            foreach (var file in files)
            {
                Encoding dbfencode = Encoding.Default;
                this.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Value++;
                    try
                    {
                        dbfencode = Encoding.GetEncoding(comboBox1.Text.Substring(0, comboBox1.Text.IndexOf('|')).Replace("\t", ""));
                    }
                    catch
                    {

                    }
                });
                bool ContainsMemo = false;
                if(dbfencode == null)
                {
                    dbfencode = Encoding.Default;
                }
                long recordCount;
                using (var dbfTable = new DbfTable(file, dbfencode))
                {
                    var header = dbfTable.Header;
                    var versionDescription = header.VersionDescription;
                    recordCount = header.RecordCount;
                    Console.WriteLine("Version: " + versionDescription + "\nRecords found:" + recordCount + "\nEncoding: " + dbfencode.BodyName);
                    var dbfRecord = new DbfRecord(dbfTable);
                    List<DbfColumnType> types = new List<DbfColumnType>();
                    List<string> field = new List<string>();
                    StringBuilder sb = new StringBuilder();
                    string tablename = file.Remove(0, file.LastIndexOf('\\') + 1).Replace(".dbf", "").Replace(".DBF", "");
                    sb.Append("CREATE TABLE " + tablename + " (\nid int NOT NULL AUTO_INCREMENT,\n");
                    foreach (var dbfColumn in dbfTable.Columns)
                    {
                        if(dbfColumn.ColumnType== DbfColumnType.Memo)
                        {
                            ContainsMemo = true;
                            if (!File.Exists(dbfTable.MemoPath()))
                            {
                                Console.WriteLine("Warning! No memo file found!");
                                ContainsMemo = false;
                            }
                        }

                        if (!dbfColumn.Name.Contains("_NullFlag"))
                        {
                            sb.Append(dbfColumn.Name.ToLower() + " ");
                            switch (dbfColumn.ColumnType)
                            {
                                case DbfColumnType.Boolean:
                                    sb.Append("tinytext,");
                                    break;
                                case DbfColumnType.Number:
                                    if (dbfColumn.DecimalCount > 0)
                                    {
                                        sb.Append("float,");
                                    }
                                    else
                                    {
                                        sb.Append("int,");
                                    }
                                    break;
                                case DbfColumnType.Float:
                                case DbfColumnType.Double:
                                    sb.Append("double,");
                                    break;
                                case DbfColumnType.SignedLong:
                                    sb.Append("bigint,");
                                    break;
                                case DbfColumnType.DateTime:
                                    sb.Append("timestamp NULL,");
                                    break;
                                case DbfColumnType.Date:
                                    sb.Append("date NULL,");
                                    break;
                                default:
                                    sb.Append("varchar(200),");
                                    break;
                            }
                            types.Add(dbfColumn.ColumnType);
                            field.Add(dbfColumn.Name);
                        }
                    }
                    sb.Append("PRIMARY KEY(ID));\n");
                    if (!string.IsNullOrEmpty(connectstring))
                    {
                        try
                        {
                            var sql = sb.ToString().Replace(",)", ")").Replace(",\n;", ";\n");
                            var cmd = new MySqlCommand(sql, con);
                            if (cmd.Connection.State != System.Data.ConnectionState.Open)
                                cmd.Connection.Open();
                            var num = cmd.ExecuteNonQuery();
                            cmd.Connection.Close();
                            Console.WriteLine("Completed! Affected rows " + num);
                        }
                        catch (Exception ex)
                        {
                            if (!ex.Message.Contains("exist"))
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }

                    }
                    else
                    {
                        File.WriteAllText(file.Replace(".dbf", ".sql").Replace(".DBF", ".sql").Replace("\\DBF","\\SQL"), sb.ToString(), Encoding.Unicode);
                    }
                    sb.Clear();
                    try
                    {
                        int rownum = 0;
                        while (dbfTable.Read(dbfRecord))
                        {
                            if (!ImportDeleted && dbfRecord.IsDeleted)
                            {
                                Console.WriteLine("Skipping deleted records.... Left " + recordCount + " rows to processed");
                                recordCount--;
                                continue;
                            }
                            try
                            {
                                if (rownum % Lines == 0)
                                {
                                    recordCount -= rownum;
                                    if (!string.IsNullOrEmpty(connectstring))
                                    {
                                        var sql = sb.ToString().Replace(",)", ")").Replace(",\n;", ";\n");
                                        if (!string.IsNullOrEmpty(sql))
                                        {
                                            var cmd = new MySqlCommand(sql, con);
                                            if (cmd.Connection.State != System.Data.ConnectionState.Open)
                                                cmd.Connection.Open();
                                            try
                                            {
                                                var num = cmd.ExecuteNonQuery();
                                                Console.WriteLine("Completed! Affected rows " + num + ". Left " + recordCount + " rows to processed");
                                            }
                                            catch
                                            {
                                                Console.WriteLine("One row failed to execute. Writting sql into file...");
                                                File.AppendAllText("error.sql", sb.ToString().Replace(",)", ")").Replace(",;", ";"), Encoding.Unicode);
                                            }
                                            cmd.Connection.Close();
                                        }
                                        sb.Clear();
                                    }
                                    else
                                    {
                                        if (sb.Length > 48064)
                                        {
                                            File.AppendAllText(file.Replace(".dbf", ".sql").Replace(".DBF", ".sql").Replace("\\DBF","\\SQL"), sb.ToString().Replace(",)", ")").Replace(",;", ";"), Encoding.Unicode);
                                            sb.Clear();
                                        }
                                    }
                                    sb.Append("INSERT INTO " + tablename + " (");
                                    foreach (var f in field)
                                    {
                                        if (f != field[field.Count - 1])
                                        {
                                            sb.Append(f + ",");
                                        }
                                        else
                                        {
                                            sb.Append(f);
                                        }
                                    }
                                    sb.Append(") VALUES ");
                                    rownum = 0;
                                }
                                sb.Append("(");
                                for (int x = 0; x < types.Count; x++)
                                {
                                    try
                                    {
                                        if (dbfRecord.Values[x].ToString().Length > 0)
                                        {
                                            if (types[x] == DbfColumnType.DateTime)
                                            {
                                                if (DateTime.TryParse(dbfRecord.Values[x].ToString(), out DateTime output))
                                                {
                                                    var stringValue = output.ToString("yyyy-MM-dd HH:mm:ss");
                                                    sb.Append("\"" + stringValue.Replace("\"", "'").Replace("\\", "/") + "\",");
                                                }
                                                else
                                                {
                                                    var stringValue = dbfRecord.Values[x].ToString();
                                                    sb.Append("\"" + stringValue.Replace("\"", "'").Replace("\\", "/") + "\",");
                                                }
                                            }
                                            else if (types[x] == DbfColumnType.Date)
                                            {
                                                var result = Convert.ToDateTime(dbfRecord.Values[x].ToString());
                                                sb.Append("\""+result.ToString("yyyy-MM-dd")+"\",");
                                            }
                                            else if(types[x] == DbfColumnType.Memo)
                                            {
                                                if (!ContainsMemo)
                                                {
                                                    sb.Append("\"null\",");
                                                }
                                                else
                                                {
                                                    var stringValue = dbfRecord.Values[x].ToString();
                                                    sb.Append("\"" + stringValue.Replace("\"", "'").Replace("\\", "/") + "\",");
                                                }
                                            }
                                            else
                                            {
                                                var stringValue = dbfRecord.Values[x].ToString();
                                                sb.Append("\"" + stringValue.Replace("\"", "'").Replace("\\", "/") + "\",");
                                            }
                                        }
                                        else
                                        {
                                            sb.Append("null,");
                                        }
                                    }
                                    catch
                                    {
                                        sb.Append("null,");
                                    }
                                }
                                rownum++;
                                sb.Append(")");
                                if (rownum % Lines == 0)
                                {
                                    sb.Append(";");
                                }
                                else
                                {
                                    sb.Append(",");
                                }
                                sb.Append("\n");
                            }
                            catch
                            {

                            }
                        }
                        sb.Length -= 2;
                        sb.Append(";");
                        if (!string.IsNullOrEmpty(connectstring))
                        {
                            var sql = sb.ToString().Replace(",)", ")").Replace(",\n;", ";\n");
                            if (!string.IsNullOrEmpty(sql))
                            {
                                var cmd = new MySqlCommand(sql, con);
                                if (cmd.Connection.State != System.Data.ConnectionState.Open)
                                    cmd.Connection.Open();
                                try
                                {
                                    var num = cmd.ExecuteNonQuery();
                                    Console.WriteLine("Completed! Affected rows " + num);
                                }
                                catch
                                {
                                    Console.WriteLine("One row failed to execute. Writting sql into file...");
                                    File.AppendAllText("error.sql", sb.ToString().Replace(",)", ")").Replace(",;", ";"), Encoding.Unicode);
                                }
                                cmd.Connection.Close();
                            }
                            sb.Clear();
                        }
                        else
                        {
                            File.AppendAllText(file.Replace(".dbf", ".sql").Replace(".DBF", ".sql").Replace("\\DBF", "\\SQL"), sb.ToString().Replace(",)", ")").Replace(",;", ";"), Encoding.Unicode);
                            sb.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                    if (!string.IsNullOrEmpty(connectstring))
                    {
                        try
                        {
                            var sql = sb.ToString().Replace(",)", ")").Replace(",\n;", ";\n");
                            if (!string.IsNullOrEmpty(sql))
                            {
                                var cmd = new MySqlCommand(sql, con);
                                if (cmd.Connection.State != System.Data.ConnectionState.Open)
                                    cmd.Connection.Open();
                                try
                                {
                                    var num = cmd.ExecuteNonQuery();
                                    Console.WriteLine("Completed! Affected rows " + num);
                                }
                                catch
                                {
                                    Console.WriteLine("One row failed to execute. Writting sql into file...");
                                    File.AppendAllText("error.sql", sb.ToString().Replace(",)", ")").Replace(",;", ";"), Encoding.Unicode);
                                }
                                cmd.Connection.Close();
                            }
                            sb.Clear();
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            Console.WriteLine("One row failed to execute. Writting sql into file...");
                            File.AppendAllText("error.sql", sb.ToString().Replace(",)", ")").Replace(",;", ";"), Encoding.Unicode);
                        }

                    }
                    else
                    {
                        File.AppendAllText(file.Replace(".dbf", ".sql").Replace(".DBF", ".sql"), sb.ToString().Replace(",)", ")"));
                    }
                    filenum++;
                }
            }
            return null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => { return ConvertData("Y"); });
            AllocConsole();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ImportDeleted = checkBox1.Checked;
        }
    }
}
