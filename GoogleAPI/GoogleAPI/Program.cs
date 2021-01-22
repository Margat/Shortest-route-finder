using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Xml;
using System.Net;
using System.Threading;

/// <summary>
/// This program uses a GoogleAPI to request the time it would take to get from one location(consumer) to several others(provider) via roads and
/// will calculate the shortest distance between each one. Naturally the location of each place should be given by the latitude and longitude.
/// This program reads from 2 .csv files labelled as DataLocations and Providers and is designed for a relatively large number of locations.
/// Important to note about this program is that it requires your own key for the API and if the number of requests are over the limit of free use 
/// then extra requests are paid for (check https://developers.google.com/maps/documentation/javascript/get-api-key).
/// </summary>
namespace ConsoleApplication4 {
    class Program {

        public static String directory = "C:/Users/User/source/repos/DistanceCalculator/Shortest-route-finder-main/";
        public static String[,] data;
        public static bool OverQueryLimit = false;
        public static int minIndex = 0;
        public static string[,] rawTable;
        public static int rowBuffer = 1;
        public static int columnBuffer = 1;
        public static string locationName = "DataLocations.csv";
        public static string providerName = "Providers.csv";
        public const int providerLatIndex = 2;
        public const int providerLongIndex = 3;
        public const int locationsLatIndex = 2;
        public const int locationsLongIndex = 3;

        /// <summary>
        /// Seperates string input by ',' and calculates the minimum distance between each value
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        static string CalculateMin(string array) {
            minIndex = 0;
            string[] distances = array.Split(',');
            for (int i = 0; i < distances.Length - 1; i++) {
                double item = Convert.ToDouble(FormatString(distances[i]));
                if (item != -1) {
                    if (item <= Convert.ToDouble(FormatString(distances[minIndex]))) {
                        minIndex = i;
                    }
                }
            }
            return distances[minIndex];
        }

        static void Main(string[] args) {
            // new Table is the output table
            String[,] newTable;
            // Read in tables
            String[,] LocationsTable = ReadTable(locationName);
            String[,] ProvidersTable = ReadTable(providerName);
            string API_key = "AIzaSyAqQxSKkvPV1m8EmyXy9XEVml477XgJNaA";

            // initialize raw data table
            if (File.Exists(directory + "DistanceMatrix.csv")) {
                rawTable = ReadTable("DistanceMatrix.csv");
            } else {
                InitializeTable(LocationsTable, ProvidersTable);
            }

            int lastInt = 0;
            // Reads newData.csv to find what line it is up to
            // This is usefull for the API so it can do API requests in chunks instead of all at once
            try {
                String[,] rawData = ReadTable("newData.csv");
                string LastID = rawData[rawData.GetLength(0) - 1, 0];
                Console.WriteLine(LastID);
                lastInt = Convert.ToInt32(LastID.Replace("Id ", "")) + 1;
            } catch {
                Console.WriteLine("Starting new file: {0}");
            }

            Decimal[] locationsPosition = new Decimal[LocationsTable.GetLength(0)];
            newTable = LocationsTable;
            String[] providers = new String[ProvidersTable.GetLength(0)];
            String[] locations = new String[LocationsTable.GetLength(0)];


            for (int i = 0; i < providers.Length; i++) {
                providers[i] = ProvidersTable[i, providerLatIndex] + "," + ProvidersTable[i, providerLongIndex];
            }
            for (int i = 0; i < locations.Length; i++) {
                locations[i] = LocationsTable[i, locationsLatIndex] + "," + LocationsTable[i, locationsLongIndex];
            }

            GetRoute(directory, locations, providers, LocationsTable, ProvidersTable, API_key, lastInt);

            SaveTable(rawTable, "DistanceMatrix.csv");

            Console.Read();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory">Base file path</param>
        /// <param name="locations">Locations array  of consumers</param>
        /// <param name="providers">Locations array of providers</param>
        /// <param name="LocationsTable">Consumers Table</param>
        /// <param name="ProvidersTable">Providers Table</param>
        /// <param name="API_key">Google API key</param>
        /// <param name="lastInt">Integer marking the last line in output table</param>
        static void GetRoute(string directory, String[] locations, String[] providers, String[,] LocationsTable, String[,] ProvidersTable, string API_key, int lastInt) {
            StreamWriter sw = new StreamWriter(directory + "newData.csv", append: true);
            Double[] distance = new Double[LocationsTable.GetLength(0)];
            string[] travelTime = new string[LocationsTable.GetLength(0)];
            // Calculate distance including route
            for (int i = lastInt; i < locations.Length; i++) {
                if (LocationsTable[i, locationsLatIndex] == "0" && LocationsTable[i, locationsLongIndex] == "0") {
                    travelTime[i] = "-1,-1";
                } else {
                    travelTime[i] = GoogleIt(locations[i], providers, providers.Length, API_key).ToString();
                }
                if (!OverQueryLimit) {
                    distance[i] = Convert.ToDouble(CalculateMin(travelTime[i]));
                    sw.WriteLine("Id {0},{1},{2},{3}", i, LocationsTable[i, 0], CalculateMin(travelTime[i]), ProvidersTable[minIndex, 0]);//, travelTime[i]);
                    sw.Flush();
                    rowBuffer = i + 1;
                    SaveRawData(travelTime[i], providers.Length);
                } else {
                    Console.Write("\nline end at Id {0}: {1}", i, travelTime[i]);
                    Console.Write("Saving Table...");
                    SaveTable(rawTable, "DistanceMatrix.csv");
                    Thread.Sleep(TimeSpan.FromSeconds(8));
                    i = i - 1;
                    OverQueryLimit = false;
                }
            }
            sw.Close();
        }

        /// <summary>
        /// Initializes Locations and Providers Table
        /// </summary>
        /// <param name="LocationsTable"></param>
        /// <param name="ProvidersTable"></param>
        static void InitializeTable(string[,] LocationsTable, string[,] ProvidersTable) {
            rawTable = new string[LocationsTable.GetLength(0) + 1, ProvidersTable.GetLength(0) + 1];
            for (int rows = 0; rows < LocationsTable.GetLength(0); rows++) {
                rawTable[rows + 1, 0] = LocationsTable[rows, 0];
            }
            for (int columns = 0; columns < ProvidersTable.GetLength(0); columns++) {
                rawTable[0, columns + 1] = ProvidersTable[columns, 0];
            }
        }

        /// <summary>
        /// Compiles array of strings with a '|' seperator
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        static string compileProviders(string[] providers) {
            string output = "";
            output += providers;
            for (int i = 1; i < providers.Length; i++) {
                output += "|" + providers[i];
            }
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerString"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        static void SaveRawData(string providerString, int length) {
            string[] providerArray = new string[length];
            providerArray = providerString.Split(',');
            if (providerArray.Length == length) {
                for (int i = 0; i < length; i++) {
                    rawTable[rowBuffer, i + 1] = providerArray[i];
                }
            } else {
                int value = -1;
                for (int i = 0; i < length; i++) {
                    rawTable[rowBuffer, i + 1] = value.ToString();
                }
            }


        }

        /// <summary>
        /// Sets column buffer
        /// </summary>
        /// <param name="location"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        static void setBuffer() {
            string[,] redTable = ReadTable("DistanceMatrix.csv");
            columnBuffer = redTable.GetLength(1) - 1;
        }



        /// <summary>
        /// Uses google maps API to determine the duration between the location and the provider. 
        /// Puts the response from the request into a Dataset then extracts and compiles the timeduration into a string.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        static string GoogleIt(string location, string[] provider, int length, string API_key) {
            // Testing key
            // string key3 = "AIzaSyAqQxSKkvPV1m8EmyXy9XEVml477XgJNaA";
            string url = @"https://maps.googleapis.com/maps/api/distancematrix/xml?units=metric&origins=" + compileProviders(provider) + "&destinations=" + location + "&key=" + API_key;
            string timeduration = "";

            string responseReader = HttpRequest(url);
            int errorCount = 0;
            DataSet ds = new DataSet();
            ds.ReadXml(new XmlTextReader(new StringReader(responseReader)));
            try {

                if (ds.Tables.Count > 0) {
                    for (int i = 0; i < length; i++) {
                        if (ds.Tables["element"].Rows[i]["status"].ToString() == "OK") {
                            timeduration += ds.Tables["distance"].Rows[i - errorCount]["value"].ToString() + ",";
                        } else {
                            url = @"https://maps.googleapis.com/maps/api/distancematrix/xml?units=metric&origins=" + provider[i] + "&destinations=" + location + "&key=" + API_key;
                            responseReader = HttpRequest(url);
                            Console.WriteLine("GoogleIt: {1} {0}", provider[i], ds.Tables["element"].Rows[i]["status"].ToString());
                            DataSet newDS = new DataSet();
                            newDS.ReadXml(new XmlTextReader(new StringReader(responseReader)));
                            errorCount += 1;
                            if (newDS.Tables["element"].Rows[0]["status"].ToString() == "OK") {
                                timeduration += newDS.Tables["distance"].Rows[0]["value"].ToString() + ",";
                            } else {
                                Console.WriteLine("error: {0}", newDS.Tables["element"].Rows[0]["status"].ToString());
                                timeduration += "-1,";
                                errorCount += 1;
                            }
                        }
                    }
                    timeduration = timeduration.Substring(0, timeduration.Length - 2);
                } else {
                    Console.WriteLine("GoogleIt: Error");
                    timeduration = "";
                }
                return timeduration;
            } catch {
                Console.WriteLine("Error: {1} LastLine: {0}", timeduration, ds.Tables["DistanceMatrixResponse"].Rows[0]["status"].ToString());
                OverQueryLimit = true;
                return timeduration;
            }


        }

        /// <summary>
        /// Opens session with website and reads response
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static string HttpRequest(string url) {
            // Setup http request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(dataStream);
            string responseReader = sr.ReadToEnd();
            return responseReader;
        }

        /// <summary>
        /// Saves the double string array to a text file
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="tableName"></param>
        static void SaveTable(String[,] Table, string tableName) {
            int rowLength = Table.GetLength(0);
            int columnLength = Table.GetLength(1);
            StreamWriter sw = new StreamWriter(directory + tableName);
            string row = "";
            // String saved table headers = Id No, School ID, Distance, ";
            for (int i = 0; i < rowLength; i++) {
                for (int j = 0; j < columnLength; j++) {
                    if (j != columnLength - 1) {
                        row += Table[i, j] + ",";
                    } else {
                        row += Table[i, j];
                    }
                }
                sw.WriteLine(row);
                row = "";
                sw.Flush();
            }
            sw.Close();
            Console.WriteLine("Table {0} has been saved", tableName);
        }

        /// <summary>
        /// returns a 2-D Arrray from Tables specified by table
        /// </summary>
        /// <param name="table">The file name to be read</param>
        /// <returns></returns>
        static string[,] ReadTable(String table) {
            String inValue = "";
            String[] column = new string[0];
            List<string> fileLine = new List<string>();
            StreamReader input = new StreamReader(directory + table);
            while ((inValue = input.ReadLine()) != null) {
                fileLine.Add(inValue);
            }

            input.Close();
            int rowLength = fileLine.Count;
            string[] arrayTest = fileLine[0].Split(',');
            int columnLength = arrayTest.Length;
            data = new string[rowLength, columnLength];
            String[] arrayLine = new string[0];
            for (int i = 0; i < fileLine.Count; i++) {
                arrayLine = fileLine[i].Split(',');

                for (int j = 0; j < arrayLine.Length; j++) {
                    data[i, j] = arrayLine[j];
                }
                if (table == "DataLocations.csv") {
                    // Console.WriteLine(data[i, 2]);
                }
            }

            return data;
        }

        /// <summary>
        /// This is simply to test the Table output
        /// </summary>
        /// <param name="Table"></param>
        static void PrintTable(String[,] Table) {
            int rowLength = Table.GetLength(0);
            int columnLength = Table.GetLength(1);
            for (int i = 0; i < rowLength; i++) {
                for (int j = 0; j < columnLength; j++) {
                    if (j != columnLength - 1) {
                        Console.Write(Table[i, j] + ",");
                    } else {
                        Console.WriteLine(Table[i, j]);
                    }
                }

            }
        }

        /// <summary>
        /// Changes the formating of the text to be a number
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static double FormatString(string str) {
            if (str == "") {
                return 0;
            }
            //try {
            int Index = str.IndexOf("'");
            if (Index != -1) {
                str = str.Substring(0, Index) + str.Substring(Index + 1, str.Length - Index); // Replaces ',' char
            }

            str = str.Replace(" ", "");
            str = str.Replace("km", "");

            string[] newStr = str.Split(',');
            int temp1 = 0;
            int temp2 = 0;
            if (newStr.Length > 1) {
                if (int.TryParse(newStr[0], out temp1)) {
                    if (int.TryParse(newStr[1], out temp2)) {
                        str = (temp1 * 60 + temp2).ToString();
                    }
                }
            }
            return Convert.ToDouble(str);
        }

    }


}
