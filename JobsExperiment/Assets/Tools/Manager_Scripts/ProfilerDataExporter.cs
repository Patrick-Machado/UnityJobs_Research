using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
public class ProfilerDataExporter : MonoBehaviour
{
    public Tayx.Graphy.Fps.G_FpsText graphy_Fps_ref;

    public bool IgnoreProfilerDataExporter = false; 


    private void Start()
    {
        Init();
    }

    Tayx.Graphy.Fps.G_FpsText.fpsData m_fps_data_ref;
    Tayx.Graphy.Fps.G_FpsText.fpsData m_fps_data;
    public void TestBreak(string m_time_test_output, ProfilerDataExporter profiler_saver, bool isAutomaticMode)
    {
        if (IgnoreProfilerDataExporter) return;

        //Save Profiler .data before manage .txts
        if (Test_Manager.instance.to_save_profiler_data) profiler_saver.ExportProfilerData();

        // Colects the FPS data from Graphy and the moment of the end of the runnning process
        m_fps_data = graphy_Fps_ref.collectFPS_data();

        //Pauses the Game Process and the Profiler
        Time.timeScale = 0;
        Profiler.enabled = false;

        // Saves the name of the test followed by its FPS and Performance Time Data:
        string test_string_name = Test_Manager.instance.GetTestInfo(Test_Manager.instance.test_index, Test_Manager.type_of_return.full_hash);
        string fps_string_data = "A_FPS: " + m_fps_data.m_avgFpsText + " Max_FPS: " + m_fps_data.m_maxFpsText + " Min_FPS: " + m_fps_data.m_minFpsText + ", ";
        string test_to_be_written_in_txt = test_string_name + ":\n" + (fps_string_data + m_time_test_output) + "\n";

        Debug.Log("A_FPS: " + m_fps_data.m_avgFpsText + " Max_FPS: " + m_fps_data.m_maxFpsText + " Min_FPS: " + m_fps_data.m_minFpsText);

        Test_Manager.instance.Ended_a_Test(test_to_be_written_in_txt, isAutomaticMode);
    }

    // Init the algorithm
    public void Init()
    {
        if (IgnoreProfilerDataExporter) return;

        bool profiler_enabled_on_config = Test_Manager.instance.to_save_profiler_data;

        ClearProfilerData();
        ChangeProfiler_ClearOnPlay(profiler_enabled_on_config);
        Profiler.enabled = profiler_enabled_on_config;

        Time.timeScale = 1;
    }

    // Set up Profiller configs such as Clear on play = true, make it enabled = true,
    //[RuntimeInitializeOnLoadMethod]
    private static void ChangeProfiler_ClearOnPlay(bool key)
    {
        EditorPrefs.SetBool("ProfilerEnabled", key);
        EditorPrefs.SetBool("ProfilerEnabled.ClearOnPlay", key);
    }

    // Clear Profiler Data for a double safety in terms of not mixing up different tests performances
    private void ClearProfilerData()
    {
        ProfilerDriver.ClearAllFrames();
    }

    //Export .data from profile with the name of the test
    public void ExportProfilerData()
    {
        if (IgnoreProfilerDataExporter) return;

        string fileName = Test_Manager.instance.GetTestInfo(Test_Manager.instance.test_index,
            Test_Manager.type_of_return.full_hash);///"profilerData";
        string fileExtension = "data";

        string filePath = GenerateUniqueFilePath(fileName, fileExtension);

        EditorApplication.delayCall += () => SaveProfilerData(filePath);
    }

    private string GenerateUniqueFilePath(string fileName, string fileExtension)
    {
        if (IgnoreProfilerDataExporter) return "";

        string aux = (Test_Manager.instance.CheckAlgorithmNumber() == true) ? "FN" : "FL";
        string baseFolderPath = "Assets/Tests/" + aux;

        string[] fileName2 = new string[1];
        fileName2 = fileName.Split('T');
        fileName2[0] += 'T';

        string fullFilePath = $"{baseFolderPath}/{fileName}.{fileExtension}";

        int fileIndex = 0;
        while (System.IO.File.Exists(fullFilePath))
        {
            fullFilePath = $"{baseFolderPath}/{fileName2[0]}{fileIndex}.{fileExtension}";
            fileIndex++;
        }

        return fullFilePath;
    }

    private void SaveProfilerData(string filePath)
    {
        if (IgnoreProfilerDataExporter) return;

        ProfilerDriver.SaveProfile(filePath);
        AssetDatabase.Refresh();
        Debug.Log("Profiler data saved to: " + filePath);
    }
}