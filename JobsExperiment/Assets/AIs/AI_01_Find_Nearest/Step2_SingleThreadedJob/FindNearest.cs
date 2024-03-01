using System.Diagnostics;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Step2
{
    public class FindNearest : MonoBehaviour
    {
        // The size of our arrays does not need to vary, so rather than create
        // new arrays every field, we'll create the arrays in Awake() and store them
        // in these fields.
        public NativeArray<float3> TargetPositions;
        public NativeArray<float3> SeekerPositions;
        public NativeArray<float3> NearestTargetPositions;


        double initialTime = 0;
        double currentTime = 0;

        private int quantity;
        ProfilerDataExporter m_profiler_data_exporter;
        public bool automatic_mode = true;

        private Stopwatch stopwatch = new Stopwatch();


        public void Awake()
        {
            m_profiler_data_exporter = GetComponent<ProfilerDataExporter>();
            SpawnGraphy();
            m_profiler_data_exporter.Init();

            if (automatic_mode == false)
            {
                //is single threaded, no need to up
                UnityEngine.Debug.Log("Manual-Mode Threads: " + (JobsUtility.JobWorkerCount + 1));
            }
            else
            {
                Update_Config(); //is single threaded but the amount of charge might vary
                UnityEngine.Debug.Log("Auto-Mode Threads: " + (JobsUtility.JobWorkerCount + 1));
            }


            Spawner spawner = Object.FindObjectOfType<Spawner>();
            // We use the Persistent allocator because these arrays must
            // exist for the run of the program.
            TargetPositions = new NativeArray<float3>(spawner.NumTargets, Allocator.Persistent);
            SeekerPositions = new NativeArray<float3>(spawner.NumSeekers, Allocator.Persistent);
            NearestTargetPositions = new NativeArray<float3>(spawner.NumSeekers, Allocator.Persistent);


            quantity = PlayerPrefs.GetInt("quantity", (int)quantitySlider.value);
            agentsText.text = "Agents: " + quantity.ToString();
            quantitySlider.SetValueWithoutNotify(quantity);
        }

        public void Start()
        {
            stopwatch.Start();
            JobsUtility.JobWorkerCount = 0;
        }

        // We are responsible for disposing of our allocations
        // when we no longer need them.
        public void OnDestroy()
        {
            TargetPositions.Dispose();
            SeekerPositions.Dispose();
            NearestTargetPositions.Dispose();
        }

        int frameCount = 0; public int maxFrames = 100;
        public void Update()
        {
            if (frameCount >= (maxFrames - 2)/*102f -2 = 100f)*/)
            {
                pause_execution();
            }


            // Copy every target transform to a NativeArray.
            for (int i = 0; i < TargetPositions.Length; i++)
            {
                // Vector3 is implicitly converted to float3
                TargetPositions[i] = Spawner.TargetTransforms[i].localPosition;
            }

            // Copy every seeker transform to a NativeArray.
            for (int i = 0; i < SeekerPositions.Length; i++)
            {
                // Vector3 is implicitly converted to float3
                SeekerPositions[i] = Spawner.SeekerTransforms[i].localPosition;
            }

            // To schedule a job, we first need to create an instance and populate its fields.
            FindNearestJob findJob = new FindNearestJob
            {
                TargetPositions = TargetPositions,
                SeekerPositions = SeekerPositions,
                NearestTargetPositions = NearestTargetPositions,
            };

            // Schedule() puts the job instance on the job queue.
            JobHandle findHandle = findJob.Schedule();

            // The Complete method will not return until the job represented by 
            // the handle finishes execution. Effectively, the main thread waits
            // here until the job is done.
            findHandle.Complete();

            // Draw a debug line from each seeker to its nearest target.
            for (int i = 0; i < SeekerPositions.Length; i++)
            {
                // float3 is implicitly converted to Vector3
                UnityEngine.Debug.DrawLine(SeekerPositions[i], NearestTargetPositions[i]);
            }


            frameCount++;
        }




        #region UI

        [Header("UI")]
        [SerializeField] private GameObject graphyPrefab = null;
        [SerializeField] private Slider quantitySlider = null;
        [SerializeField] private TextMeshProUGUI agentsText = null;

        private void SpawnGraphy()
        {

            GameObject graphy = Instantiate(graphyPrefab);
            //Invoke("pause_execution", execution_time_seconds);

            m_profiler_data_exporter.graphy_Fps_ref = graphy.transform.GetChild(0).GetComponent<Tayx.Graphy.Fps.G_FpsText>();
        }

        public void OnRestartClicked()
        {
            PlayerPrefs.SetInt("quantity", (int)quantitySlider.value);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        #endregion


        void pause_execution()
        {
            stopwatch.Stop();
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            currentTime = elapsedMilliseconds;

            UnityEngine.Debug.Log("Finished Execution, Execution Time: " + /*(currentTime - initialTime)*/ currentTime + " Frames: " + maxFrames + " Media ET/F: " + (/*(currentTime - initialTime)*/ currentTime / maxFrames));
            //Debug.Break();
            string test_string_time = "Finished Execution, Execution Time: " + currentTime + " Frames: " + maxFrames + " Media ET/F: " + (currentTime / maxFrames);
            m_profiler_data_exporter.TestBreak(test_string_time, m_profiler_data_exporter, automatic_mode);
        }


        void RestartScene()
        {
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        public void Update_Config()
        {
            int test_id = Test_Manager.instance.test_index;
            int amount_units; int amount_threads = 1; //int amount_frames;

            amount_units = int.Parse(Test_Manager.instance.GetTestInfo(test_id, Test_Manager.type_of_return.problem_size));
            //amount_threads = int.Parse(Test_Manager.instance.GetTestInfo(test_id, Test_Manager.type_of_return.thread_count));

            // Update amount of Worker Threads
            JobsUtility.JobWorkerCount = (amount_threads -1); // this is single thread therefore zero which is one thread only
            UnityEngine.Debug.Log("Threads: " + (JobsUtility.JobWorkerCount + 1));

            // Charge change
            Spawner s = GetComponent<Spawner>();
            s.NumSeekers = amount_units;
            s.NumTargets = amount_units;

            // Update frames
            Test_Manager.type_of_return frames = Test_Manager.type_of_return.frames;
            string sMaxFrames = Test_Manager.instance.GetTestInfo(test_id, frames);
            maxFrames = int.Parse(sMaxFrames);

        }

    }
}