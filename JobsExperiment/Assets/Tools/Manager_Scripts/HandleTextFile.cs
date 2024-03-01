using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


public class HandleTextFile : MonoBehaviour
{
    public string Data;
    public HandleTextFile(string m_path)
    {
        path = m_path;
    }
    public void Start()
    {
        //WriteString("1", "0", true);
        //ReadString("0");

        //WriteOnTxt();
        //LoadFromTxt();
        //Debug.Log(Data);
    }

    // Path to the file.
    public string path = "Assets/Tests/FN/Results_FN.txt";

    public void WriteToFile(string data)
    {
        //Create a StreamWriter and write text to the file
        using (StreamWriter writer = new StreamWriter(path, true))
        {
            writer.WriteLine(data);
        }
    }

    void WriteOnTxt()
    {
        HandleTextFile writer = new HandleTextFile(path);
        //writer.WriteToFile("This is some data");

    }

    void LoadFromTxt()
    {
        HandleTextFile writer = new HandleTextFile(path);
        writer.ReadFromFile();
    }

    public void ReadFromFile()
    {
        // Check if the file exists before reading.
        if (File.Exists(path))
        {
            // Create a StreamReader and read text from the file.
            using (StreamReader reader = new StreamReader(path))
            {
                // Loop through all lines in the file.
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    Data = line;
                }
            }
        }
        else
        {
            Debug.Log("File does not exist... Please create a File in: " + path);
        }
    }

    /*[MenuItem("Tools/Write file")]
    public static void WriteString(string value, string fileId, bool lineEnter)
    {
        string path = "Assets/Resources/data_" + fileId + ".txt";
        //path = "Assets/Resources/test.txt";
        StreamWriter writer = new StreamWriter(path, true);
        if (lineEnter) { writer.WriteLine(value); }
        else { writer.Write(value); }
        writer.Close();
    }

    public static void UpdateDataOnEditor(string fileId)
    {
        string path = "Assets/Resources/data_" + fileId + ".txt";
        AssetDatabase.ImportAsset(path);
    }

    [MenuItem("Tools/Read file")]
    public static void ReadString(string fileID)
    {

        string path = "Assets/Resources/data_" + fileID + ".txt";

        StreamReader reader = new StreamReader(path);
        Data = (reader.ReadToEnd());
        reader.Close();
    }

 */

}
