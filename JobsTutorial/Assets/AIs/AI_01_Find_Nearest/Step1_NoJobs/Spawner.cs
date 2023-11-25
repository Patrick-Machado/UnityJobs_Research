using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Diagnostics;

namespace Step1
{
    public class Spawner : MonoBehaviour
    {
        // The set of targets is fixed, so rather than 
        // retrieve the targets every frame, we'll cache 
        // their transforms in this field.
        public static Transform[] TargetTransforms;

        public GameObject SeekerPrefab;
        public GameObject TargetPrefab;
        public int NumSeekers;
        public int NumTargets;
        public Vector2 Bounds;

        double currentTime = 0;

        ProfilerDataExporter m_profiler_data_exporter;
        public bool automatic_mode = true;

        private Stopwatch stopwatch = new Stopwatch();

        public void Awake()
        {
            m_profiler_data_exporter = GetComponent<ProfilerDataExporter>();

            SpawnGraphy();
            m_profiler_data_exporter.Init();

            UnityEngine.Random.InitState(123);

            if (automatic_mode)
            { Update_Config(); }

            for (int i = 0; i < NumSeekers; i++)
            {
                GameObject go = GameObject.Instantiate(SeekerPrefab);
                Seeker seeker = go.GetComponent<Seeker>();
                Vector2 dir = UnityEngine.Random.insideUnitCircle;
                seeker.Direction = new Vector3(dir.x, 0, dir.y);
                go.transform.localPosition = new Vector3(
                UnityEngine.Random.Range(0, Bounds.x), 0, UnityEngine.Random.Range(0, Bounds.y));
            }

            TargetTransforms = new Transform[NumTargets];
            for (int i = 0; i < NumTargets; i++)
            {
                GameObject go = GameObject.Instantiate(TargetPrefab);
                Target target = go.GetComponent<Target>();
                Vector2 dir = UnityEngine.Random.insideUnitCircle;
                target.Direction = new Vector3(dir.x, 0, dir.y);
                TargetTransforms[i] = go.transform;
                go.transform.localPosition = new Vector3(
                UnityEngine.Random.Range(0, Bounds.x), 0, UnityEngine.Random.Range(0, Bounds.y));
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
        void Update()
        {
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
            Spawner s = GetComponent<Spawner>();
            s.NumSeekers = amount_units;
            s.NumTargets = amount_units;

            // Update frames
            Test_Manager.type_of_return frames = Test_Manager.type_of_return.frames;
            maxFrames = int.Parse(Test_Manager.instance.GetTestInfo(test_id, frames));
        }
    }
}
