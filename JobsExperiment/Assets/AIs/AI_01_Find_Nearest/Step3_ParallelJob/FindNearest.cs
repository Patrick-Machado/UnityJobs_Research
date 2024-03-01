using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Jobs.LowLevel.Unsafe;
using System;
using System.Diagnostics;

namespace Step3
{
    public class FindNearest : MonoBehaviour
    {
        public NativeArray<float3> TargetPositions;
        public NativeArray<float3> SeekerPositions;
        public NativeArray<float3> NearestTargetPositions;
        private int quantity;

        double initialTime = 0;
        double currentTime = 0;

        public int BatchSize = 64;
        public int ThreadLimitedTo = -1;

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
                if (ThreadLimitedTo != -1) JobsUtility.JobWorkerCount = ThreadLimitedTo - 1;
                UnityEngine.Debug.Log("Manual-Mode Threads: " + (JobsUtility.JobWorkerCount + 1));
            }
            else
            {
                Update_Config();
                UnityEngine.Debug.Log("Auto-Mode Threads: " + (JobsUtility.JobWorkerCount + 1));
            }

            Spawner spawner = UnityEngine.Object.FindObjectOfType<Spawner>();
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
        }
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

            for (int i = 0; i < TargetPositions.Length; i++)
            {
                TargetPositions[i] = Spawner.TargetTransforms[i].localPosition;
            }

            for (int i = 0; i < SeekerPositions.Length; i++)
            {
                SeekerPositions[i] = Spawner.SeekerTransforms[i].localPosition;
            }

            FindNearestJob findJob = new FindNearestJob
            {
                TargetPositions = TargetPositions,
                SeekerPositions = SeekerPositions,
                NearestTargetPositions = NearestTargetPositions,
            };

            // Execute will be called once for every element of the SeekerPositions array,
            // with every index from 0 up to (but not including) the length of the array.
            // The Execute calls will be split into batches of 64.
            JobHandle findHandle = findJob.Schedule(SeekerPositions.Length, BatchSize);

            findHandle.Complete();

            for (int i = 0; i < SeekerPositions.Length; i++)
            {
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

            UnityEngine.Debug.Log("Finished Execution, Execution Time: " + currentTime + " Frames: " + maxFrames + " Media ET/F: " + (currentTime / maxFrames));
            string test_string_time = "Finished Execution, Execution Time: " + currentTime + " Frames: " + maxFrames + " Media ET/F: " + (currentTime / maxFrames);
            m_profiler_data_exporter.TestBreak(test_string_time, m_profiler_data_exporter, automatic_mode);
        }

        public void Update_Config()
        {
            int test_id = Test_Manager.instance.test_index;
            int amount_units; int amount_threads; //int amount_frames;
            BatchSize = Test_Manager.instance.Gloabal_BatchSize;

            amount_units = int.Parse(Test_Manager.instance.GetTestInfo(test_id, Test_Manager.type_of_return.problem_size));
            amount_threads = int.Parse(Test_Manager.instance.GetTestInfo(test_id, Test_Manager.type_of_return.thread_count));

            // Update amount of Worker Threads
            JobsUtility.JobWorkerCount = amount_threads - 1;
            //Debug.Log("Threads: " + (JobsUtility.JobWorkerCount + 1));

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