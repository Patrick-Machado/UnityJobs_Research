using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class Test_Manager : MonoBehaviour
{
    public int amount_tests_per_config = 12; // How many times a config will be tested/ran 
    public int amount_frames_per_test = 100; // How many Frames each test will have
    public bool to_save_profiler_data = false; //If true will use save profiler to store more data
    public List<int> problem_size_variations =     // Proablem Length variation for each test execution (1440 first, then 1200, then 960... for every thread variation)
                        new List<int> { 1440, 1200, 960, 720, 480, 240, 120 };
    public List<int> scalability_thread_variation =// Variation of threads per test config, 1440 units with 8 threads, then with 6 threads... if 1 it will be sequential instead of parallel with one singlethread
                        new List<int> { 8, 6, 4, 2, 1 };
    public int amount_of_scenes_to_be_teste = 4;// number of scenes ex: Flocking: parallel = scene 0, sequential = scene 1. Find Nearest: parallel = scene 2, sequential = scene 3. etc
    public int amount_of_sequential_scenes = 2;// integer used to be math calculated of the ammount of sequential scenes to be ran, since scenes doesn't have the same amount of test as the parallel ones
    public int Gloabal_BatchSize = 1; // 8, 16, 32, or most recomended 64

    public static Test_Manager instance;

    private void Awake()
    {
        #region Singleton_Related
        // Check if an instance already exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        #endregion

        Calculate_maxtests_int();

        Init_Test();
    }

    public int test_index = 0;
    public int max_tests = 0;

    void Calculate_maxtests_int()
    {
        // Expect the inspector to be with one element equals 1, which means that it expects pairs of thread variation elements and
        // always a final thread variation element with 1: example: 8, 6, 4, 2, 1. Which means 8 threads, then 6 threads, 4, threads,
        // 2 threads, and the Sequential Batch of tests. Do not change the order, the 1 or Sequential, must be at the end.
        max_tests = ((amount_tests_per_config * (problem_size_variations.Count /* -1 (One of them is parallel so -1)*/)
            * (scalability_thread_variation.Count - 1) /*(One of them is the sequential so -1) */) * (amount_of_scenes_to_be_teste - amount_of_sequential_scenes /*(A few of them are meant to be sequential so -X, since sequentially they only iterates referred to the problem size variation)*/))
            + ((amount_of_sequential_scenes * problem_size_variations.Count /*This doesnt subtract since the sequentila will variates between every problem size variation)*/) * amount_tests_per_config /*(Since the sequential will be executed X times for each config)*/);

    }

    public void Init_Test()
    {
        Calculate_maxtests_int();

        #region GuideForCalcOfTests_Comments
        // Config of 12 tests: Note(if different amount of tests per config, adjust the logic from X to X instead of from 12 to 12)

        //_Scene 1 - Algorithm 1 (Parallel):
        // Batch 1;
        // 000-011 (1440u 8t, Parallel, Alg 1);
        // 012-023 (1440u 6t, Parallel, Alg 1);
        // 024-035 (1440u 4t, Parallel, Alg 1);
        // 036-047 (1440u 2t, Parallel, Alg 1);

        // Batch 2;
        // 048-059 (1200u 8t, Parallel, Alg 1);
        // 060-071 (1200u 6t, Parallel, Alg 1);
        // 072-083 (1200u 4t, Parallel, Alg 1);
        // 084-095 (1200u 2t, Parallel, Alg 1);

        // Batch 3;
        // 096-107 (960u 8t, Parallel, Alg 1);
        // 108-119 (960u 6t, Parallel, Alg 1);
        // 120-131 (960u 4t, Parallel, Alg 1);
        // 132-143 (960u 2t, Parallel, Alg 1);

        // Batch 4;
        // 144-155 (720u 8t, Parallel, Alg 1);
        // 156-167 (720u 6t, Parallel, Alg 1);
        // 168-179 (720u 4t, Parallel, Alg 1);
        // 180-191 (720u 2t, Parallel, Alg 1);

        // Batch 5;
        // 192-203 (480u, 8t, Parallel, Alg 1);
        // 204-215 (480u, 6t, Parallel, Alg 1);
        // 216-227 (480u, 4t, Parallel, Alg 1);
        // 228-239 (480u, 2t, Parallel, Alg 1);

        // Batch 6;
        // 240-251 (240u, 8t, Parallel, Alg 1);
        // 252-263 (240u, 6t, Parallel, Alg 1);
        // 264-275 (240u, 4t, Parallel, Alg 1);
        // 276-287 (240u, 2t, Parallel, Alg 1);

        // Batch 7;
        // 288-299 (120u, 8t, Parallel, Alg 1);
        // 300-311 (120u, 6t, Parallel, Alg 1);
        // 312-323 (120u, 4t, Parallel, Alg 1);
        // 324-335 (120u, 2t, Parallel, Alg 1);

        //Batch 8;
        //_Scene 2 - Algorithm 1 (Sequential):
        // 336-347  (1440u, 1t, Sequential, Alg 1);
        // 348-359  (1200u, 1t, Sequential, Alg 1);
        // 360-371  ( 960u, 1t, Sequential, Alg 1);
        // 372-383  ( 720u, 1t, Sequential, Alg 1);
        // 384-395  ( 480u, 1t, Sequential, Alg 1);
        // 396-407  ( 240u, 1t, Sequential, Alg 1);
        // 408-419  ( 120u, 1t, Sequential, Alg 1);


        //... 839//

        // ... Next algorithm same logic with 7 Parallel Batches
        // and one Sequential Batch, until hit the index 839.
        // PS: that this quantity of 839 is based on the
        // config input of data entry in the inspector.
        #endregion


        Invoke("LoadSceneWithDelay", 5f);// This timing is only on the beginning and dont affect timing calculation, since the scene who starts the time counting
    }

    void LoadSceneWithDelay()
    {
        Debug.Log("LoadSceneWithDelay");
        StartOrReloadScene();
    }
    public bool CheckAlgorithmNumber()// FindNearest = true, Flocking = false;
    {
        return (test_index < (max_tests / amount_of_sequential_scenes/*420*/)) ? true : false;
    }

    public string GetTestInfo(int test_index, type_of_return type_of_data_expected)
    {
        string algorithm = (test_index < (max_tests / amount_of_sequential_scenes/*420*/)) ? "Alg_1" : "Alg_2";
        string scene;
        int problem_size;
        int thread_count;
        int test_number = (test_index % amount_tests_per_config) + 1;  // Test number varies from 1 to 12 for each batch

        // Scene determination: Parallel or Sequential
        if (test_index < /*(840/2 =420-84 =336)*/ ((max_tests / amount_of_sequential_scenes/*420*/) - (problem_size_variations.Count * amount_tests_per_config))

            || test_index >= (max_tests / amount_of_sequential_scenes/*420*/)
            && test_index < /*(840-84 =756)*/ max_tests - (problem_size_variations.Count * amount_tests_per_config/*(7*12 =84)*/))

        {
            scene = "Parallel";

            // Determining batch index and then setting problem size and thread count
            int batchIndex = 0;
            int threadVariationIndex = 0;
            if (test_index >= (max_tests / amount_of_sequential_scenes))
            {
                int new_testIndex_virtual = test_index - (max_tests / amount_of_sequential_scenes); /*336*/
                batchIndex = new_testIndex_virtual / (/*(problem_size_variations.Count - 1) **/ (scalability_thread_variation.Count - 1) * amount_tests_per_config);
                threadVariationIndex = (new_testIndex_virtual % ((scalability_thread_variation.Count - 1) * amount_tests_per_config) /*Default 48*/) / amount_tests_per_config; // There are 12 tests for each thread variation in a batch
            }
            else
            {
                batchIndex = test_index / (/*(problem_size_variations.Count - 1) **/ (scalability_thread_variation.Count - 1) * amount_tests_per_config); // 48 tests per batch (4 thread variations * 12 tests per config)
                threadVariationIndex = (test_index % ((scalability_thread_variation.Count - 1) * amount_tests_per_config) /*Default 48*/) / amount_tests_per_config; // There are 12 tests for each thread variation in a batch
            }
            problem_size = problem_size_variations[batchIndex];

            thread_count = scalability_thread_variation[threadVariationIndex];
        }
        else
        {
            scene = "Sequential";
            thread_count = 1;  // In Sequential scene, thread count is always 1

            int batchIndexSequential = 0;
            if (test_index < (/*840*/ max_tests / amount_of_sequential_scenes))
            {
                // Determining batch index for Sequential scene and then setting problem size
                batchIndexSequential =
                   (test_index - /*336*/((max_tests / amount_of_sequential_scenes/*420*/)
                   - (problem_size_variations.Count *
                   amount_tests_per_config))) / amount_tests_per_config; // 12 tests per problem size in Sequential scene
            }
            else
            {
                batchIndexSequential =
                    (test_index - ((max_tests/*840*/)
                    - (problem_size_variations.Count *
                    amount_tests_per_config))) / amount_tests_per_config; // 12 tests per problem size in Sequential scene

            }
            problem_size = problem_size_variations[batchIndexSequential];
        }

        // Building the result string
        string result = $"{algorithm}_{problem_size}u_{thread_count}t_{scene}_{amount_frames_per_test}f_T{test_number}";

        if (type_of_data_expected == type_of_return.full_hash) return result;
        if (type_of_data_expected == type_of_return.algorithm) return algorithm.ToString();
        if (type_of_data_expected == type_of_return.problem_size) return problem_size.ToString();
        if (type_of_data_expected == type_of_return.thread_count) return thread_count.ToString();
        if (type_of_data_expected == type_of_return.scene) return scene.ToString();
        if (type_of_data_expected == type_of_return.frames) return amount_frames_per_test.ToString();
        if (type_of_data_expected == type_of_return.test_number) return test_number.ToString();
        return result;
    }

    public enum type_of_return
    {
        full_hash, algorithm, problem_size, thread_count, scene, test_number, frames
    }

    #region txt_related
    // Method to open and read a text file
    public static string OpenTextFile(string filePath)
    {
        string aux = (Test_Manager.instance.CheckAlgorithmNumber() == true) ? "FN" : "FL";
        string baseFolderPath = "Assets/Tests/" + aux;

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File at {filePath} does not exist.");
        }

        return File.ReadAllText(filePath);
    }

    //public HandleTextFile m_handleTextFile;
    // Method to save a text file
    public static void SaveTextFile(string filePath, string content)
    {
        HandleTextFile m_handleTextFile = new HandleTextFile(filePath);

        m_handleTextFile.WriteToFile(content);
    }

    #endregion


    public void Ended_a_Test(string final_2be_saved_in_txt, bool isAutomaticMode)
    {
        string aux = "Results_";
        if (test_index < (max_tests / amount_of_sequential_scenes)/*210*/)
        { aux += "FN"; }
        else { aux += "FL"; }


        string aux2 = (Test_Manager.instance.CheckAlgorithmNumber() == true) ? "FN" : "FL";
        string baseFolderPath = "Assets/Tests/" + aux2 + "/" + aux + ".txt";

        HandleTextFile handleTextFile = new HandleTextFile(baseFolderPath);

        handleTextFile.ReadFromFile();
        if (handleTextFile.Data == null) { handleTextFile.WriteToFile("T" + test_index); }
        string lastLine = handleTextFile.Data;
        bool matchesPattern = Regex.IsMatch(lastLine, @"^T\d+$");

        if (matchesPattern)
        {
            if (max_tests == 0) Calculate_maxtests_int();

            if (test_index < max_tests)
            {
                // Remove the line Tn
                List<string> lines = File.ReadAllLines(baseFolderPath).ToList();
                if (lines.Count > 0)
                {
                    // Remove last line if it matches pattern
                    if (Regex.IsMatch(lines.Last(), @"^T\d+$"))
                    {
                        lines.RemoveAt(lines.Count - 1);
                        File.WriteAllLines(baseFolderPath, lines);
                    }
                }
                //Write other Tn
                SaveTextFile(baseFolderPath, final_2be_saved_in_txt);
                SaveTextFile(baseFolderPath, "T" + (test_index));
            }
            else
            {
                SaveTextFile(baseFolderPath, "\n Tests Finished");
            }
        }
        else
        {
            // First test
            SaveTextFile(baseFolderPath, final_2be_saved_in_txt);
            SaveTextFile(baseFolderPath, "T" + (test_index));
        }


        test_index += 1;

        if (isAutomaticMode == false) { Debug.Log("Done Iteration"); Debug.Break(); }

        if (test_index >= max_tests) { Debug.Log("Tests Finished! Congratulations!!!"); Debug.Break(); }

        StartOrReloadScene();

    }

    void StartOrReloadScene()
    {

        if (test_index < max_tests)
        {
            // Scene 1 - Parallel FN:
            if (test_index < /*(840/2 =420-84 =336)*/ ((max_tests / amount_of_sequential_scenes/*420*/) - (problem_size_variations.Count * amount_tests_per_config /*84*/)))
            {
                SceneManager.LoadScene(0);
            }
            // Scene 2 - Sequential FN:
            else if (test_index >= /*(840/2 =420-84 =336)*/ ((max_tests / amount_of_sequential_scenes/*420*/) - (problem_size_variations.Count * amount_tests_per_config /*84*/))
                && test_index < (max_tests / (amount_of_sequential_scenes)) /*420*/)
            {
                SceneManager.LoadScene(1);
            }
            // Scene 3 - Parallel FL:
            else if (test_index >= max_tests / (amount_of_sequential_scenes) /*420*/
                && test_index < (max_tests - (problem_size_variations.Count * amount_tests_per_config /*84*/)))
            {
                SceneManager.LoadScene(2);
            }
            // Scene 4 - Sequential FL:
            else if (test_index >= (/*756*/max_tests - (problem_size_variations.Count * amount_tests_per_config /*84*/))
                && test_index < max_tests)
            {
                SceneManager.LoadScene(3);
            }
        }

    }

}
