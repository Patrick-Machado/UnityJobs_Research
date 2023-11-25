using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Diagnostics;

namespace NewBoid_Sequential
{
    public class FlockManager_Sequential : MonoBehaviour
    {
        public GameObject boidPrefab;
        public int numBoids = 50;
        public float maxSpeed = 5f;
        public float maxForce = 1f;
        public float separationDistance = 2f;
        public float alignmentDistance = 10f;
        public float cohesionDistance = 10f;
        public float separationWeight = 1f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 1f;
        public float boundsRadius = 50f;
        public Transform target;
        public GameObject[] boidPrefabs;
        public float spawnRadius;

        double currentTime = 0;

        ProfilerDataExporter m_profiler_data_exporter;
        public bool automatic_mode = true;

        private Stopwatch stopwatch = new Stopwatch();

        void Awake()
        {
            m_profiler_data_exporter = GetComponent<ProfilerDataExporter>();

            SpawnGraphy();
            m_profiler_data_exporter.Init();

            if (automatic_mode)
            { Update_Config(); }

            boidPrefabs = new GameObject[numBoids];
            for (int i = 0; i < numBoids; i++)
            {
                // Instantiate the boid prefab and store the reference in boidPrefabs
                Vector3 position = new Vector3(UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius));
                Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range(-180, 180), UnityEngine.Random.Range(-180, 180), UnityEngine.Random.Range(-180, 180));
                GameObject _boidPrefab = Instantiate(boidPrefab, position, rotation);
                boidPrefabs[i] = _boidPrefab;
                _boidPrefab.GetComponent<BoidBehaviour>().flockManager = this;
            }

            quantity = PlayerPrefs.GetInt("quantity", (int)quantitySlider.value);
            agentsText.text = "Agents: " + quantity.ToString();
            quantitySlider.SetValueWithoutNotify(quantity);
        }

        public void Start()
        {
            stopwatch.Start();
        }

        int frameCount = 0; public int maxFrames = 100;
        private void Update()
        {
            //currentTime = Time.realtimeSinceStartup - initialTime;
            if (frameCount >= (maxFrames - 2)/*102f -2 = 100f)*/)
            {
                pause_execution();
            }

            frameCount++;
        }

        #region UI

        [Header("UI")]
        [SerializeField] private GameObject graphyPrefab = null;
        [SerializeField] private Slider quantitySlider = null;
        [SerializeField] private TextMeshProUGUI agentsText = null;
        private int quantity;
        [SerializeField] float execution_time_seconds = 10;
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
            int amount_units; //int amount_threads; //int amount_frames;

            amount_units = int.Parse(Test_Manager.instance.GetTestInfo(test_id, Test_Manager.type_of_return.problem_size));

            // Charge change
            numBoids = amount_units;

            // Update frames
            Test_Manager.type_of_return frames = Test_Manager.type_of_return.frames;
            maxFrames = int.Parse(Test_Manager.instance.GetTestInfo(test_id, frames));
        }
    }
}