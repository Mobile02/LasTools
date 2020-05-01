using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace LAS
{
    public class LasTools
    {
        public string Path { private get; set; }
        public DataTable DataTable { get; set; }

        private readonly Dictionary<string, string> nameCurves = new Dictionary<string, string>
        {
            ["DEPTH"] = " .M                              DEPTH",
            ["TVD"] = " .M                                TRUE VERTICAL DEPTH",
            ["RACECHM"] = " .ohm-m                        RESISTIVITY ATTEN. BHC 2 MHZ LONG",
            ["RACECLM"] = " .ohm-m                        RESISTIVITY ATTEN. BHC 400 KHZ LONG",
            ["RACECSHM"] = " .ohm-m                       RESISTIVITY ATTEN. BHC 2 MHZ SHORT",
            ["RACECSLM"] = " .ohm-m                       RESISTIVITY ATTEN. BHC 400 KHZ SHORT",
            ["RPCECHM"] = " .ohm-m                        RESISTIVITY PHASE DIFF. BHC 2 MHZ LONG",
            ["RPCECLM"] = " .ohm-m                        RESISTIVITY PHASE DIFF. BHC 400 KHZ LONG",
            ["RPCECSHM"] = " .ohm-m                       RESISTIVITY PHASE DIFF. BHC 2 MHZ SHORT",
            ["RPCECSLM"] = " .ohm-m                       RESISTIVITY PHASE DIFF. BHC 400 KHZ SHORT",
            ["GRCM"] = " .API                             GAMMA RAY BOREHOLE CORRECTED"
        };

        public LasTools()
        {

        }

        public LasTools(string path)
        {
            Path = path;
            DataTable = new DataTable();
        }

        public LasTools(string path, DataTable dataTable)
        {
            Path = path;
            DataTable = dataTable;
        }
        public void LoadFromFile()
        {
            FillTable();
            //dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns["DEPTH"] };
        }

        private List<string> GetInfoAboutCurves()   // Получаем массив строк с информацией о названиях кривых (нужно будет для названий столбцов)
        {
            List<string> listCurves = new List<string> { };
            StreamReader streamReader = new StreamReader(Path);

            while (!streamReader.EndOfStream)
            {
                string stringFromFile = streamReader.ReadLine();

                if (stringFromFile.StartsWith("~C"))
                {
                    stringFromFile = streamReader.ReadLine();

                    while (!stringFromFile.StartsWith("~"))
                    {
                        if (!stringFromFile.StartsWith("#"))
                        {
                            listCurves.Add(stringFromFile);
                            stringFromFile = streamReader.ReadLine();
                        }
                        else
                            stringFromFile = streamReader.ReadLine();
                    }
                }
            }

            streamReader.Close();
            return listCurves;
        }

        public List<string> GetHeader()    // Получаем весь заголовок LAS файла
        {
            List<string> listHeader = new List<string> { };
            StreamReader streamReader = new StreamReader(Path);

            while (!streamReader.EndOfStream)
            {
                string stringFromFile = streamReader.ReadLine();
                listHeader.Add(stringFromFile);

                if (stringFromFile.StartsWith("~A"))
                {
                    streamReader.Close();
                    return listHeader;
                }
            }

            streamReader.Close();
            return listHeader;
        }

        public bool SaveFile()  // Сохраняем LAS файл
        {
            StreamWriter streamWriter = new StreamWriter(Path);

            var listHeader = CreateHeader();    // Создаем заголовок LAS файла

            for (int i = 0; i < listHeader.Count; i++)     // Записываем в файл заголовок
            {
                streamWriter.WriteLine(listHeader[i]);
            }

            for (int i = 0; i < DataTable.Rows.Count; i++) // Записываем в файл данные
            {
                string stringForWrite = "";

                for (int j = 0; j < DataTable.Columns.Count; j++)
                {
                    stringForWrite += "   " + FormattingString(DataTable.Rows[i][j].ToString());
                }
                streamWriter.WriteLine(stringForWrite, true, Encoding.UTF8);
            }

            streamWriter.Close();
            return true;
        }

        private List<string> CreateHeader()
        {
            List<string> listHeader = new List<string> { };

            listHeader.Add("~VERSION INFORMATION SECTION");
            listHeader.Add("VERS.           2.0     :   CWLS log ASCII Standard - VERSION 2.0");
            listHeader.Add("WRAP.NO      :   One line per depth step");
            listHeader.Add("#--------------------------------------------------");
            listHeader.Add("#-----------------------------------------------------------------------------");
            listHeader.Add("~CURVE INFORMATION SECTION");
            listHeader.Add("#MNEM.UNIT         API CODE      : CURVE  DESCRIPTION");
            listHeader.Add("#---- -----        -----------   --------------------------------");

            for (int i = 0; i < DataTable.Columns.Count; i++)   // Добавляем в заголовок информацию о кривых
            {
                listHeader.Add(FormattingStringCurve(DataTable.Columns[i].ColumnName));
            }

            listHeader.Add("#-----------------------------------------------------------------------------");
            listHeader.Add("~PARAMETER INFORMATION SECTION");
            listHeader.Add("#MNEM.UNIT                  VALUE                DESCRIPTION");
            listHeader.Add("#-----------------------------------------------------------------------------");
            listHeader.Add("~A");
            return listHeader;
        }

        private void CreateColumn(List<string> columnName)
        {
            DataColumn column = new DataColumn();

            for (int i = 0; i < columnName.Count; i++)  // Создаем и обзываем столбцы (в названии не должно быть пробелов)
            {
                string tmp;
                if (columnName[i].IndexOf(" ") >= 0) // Проверка на наличие пробелов в будущем названии столбца, если пробелы есть, то вырезаем все начиная с первого пробела
                    tmp = columnName[i].Remove(columnName[i].IndexOf(" "));
                else
                    tmp = columnName[i];

                column.DataType = Type.GetType("System.String");
                try
                {
                    DataTable.Columns.Add(tmp, typeof(String));
                }
                catch (DuplicateNameException)
                {
                    DataTable.Columns.Add(tmp + "copy", typeof(String));
                }
            }
        }

        private void FillTable()
        {
            string stringFromFile;
            string[] arrayFromString;
            DataRow row;
            StreamReader streamReader = new StreamReader(Path);

            CreateColumn(GetInfoAboutCurves());
            
            while (!streamReader.EndOfStream)
            {
                stringFromFile = streamReader.ReadLine();

                if (stringFromFile.StartsWith("~A"))
                {
                    while (!streamReader.EndOfStream)
                    {
                        stringFromFile = streamReader.ReadLine();
                        arrayFromString = stringFromFile.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        row = DataTable.NewRow();

                        for (int i = 0; i < arrayFromString.Length; i++)
                        {
                            row[i] = arrayFromString[i];
                        }

                        DataTable.Rows.Add(row);
                    }
                }
            }
            streamReader.Close();
        }

        private string FormattingString(string stringForWrite)
        {
            string space = "";
            for (int a = 0; a < 9 - stringForWrite.Length; a++)
            {
                space += " ";
            }
            stringForWrite += space;
            return stringForWrite;
        }

        private string FormattingStringCurve(string rowString)
        {
            try
            {
                string formattedString = rowString + nameCurves[rowString];
                return formattedString;
            }
            catch (KeyNotFoundException)
            {
                return rowString;
            }
        }
    }
}
