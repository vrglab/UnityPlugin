using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModTagContainer : ModTagCollectionDisplay
    {
        // ---------[ FIELDS ]---------
        public delegate void OnTagClicked(ModTagDisplay display, string tagName, string category);
        public event OnTagClicked tagClicked;

        [Header("Settings")]
        public GameObject tagDisplayPrefab;

        [Header("UI Components")]
        public RectTransform container;
        public GameObject loadingOverlay;

        // --- RUNTIME DATA ---
        private int m_modId = -1;
        private List<ModTagDisplay> m_tagDisplays = new List<ModTagDisplay>();

        // --- ACCESSORS ---
        public IEnumerable<ModTagDisplay> tagDisplays { get { return m_tagDisplays; } }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            Debug.Assert(container != null);
            Debug.Assert(tagDisplayPrefab != null);
            Debug.Assert(tagDisplayPrefab.GetComponent<ModTagDisplay>() != null);
        }

        public void OnEnable()
        {
            StartCoroutine(LateUpdateLayouting());
        }

        public System.Collections.IEnumerator LateUpdateLayouting()
        {
            yield return null;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories)
        {
            DisplayModTags(-1, tags, tagCategories);
        }
        public override void DisplayModTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(profile != null);
            DisplayModTags(profile.id, profile.tagNames, tagCategories);
        }
        public override void DisplayModTags(int modId, IEnumerable<string> tags,
                                            IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(tags != null);

            m_modId = modId;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            // clear
            foreach(ModTagDisplay display in m_tagDisplays)
            {
                GameObject.Destroy(display.gameObject);
            }
            m_tagDisplays.Clear();

            // create
            IDictionary<string, string> tagCategoryMap
                = ModTagCollectionDisplay.GenerateTagCategoryMap(tags, tagCategories);

            foreach(var tagCategory in tagCategoryMap)
            {
                GameObject displayGO = GameObject.Instantiate(tagDisplayPrefab,
                                                              new Vector3(),
                                                              Quaternion.identity,
                                                              container);

                ModTagDisplay display = displayGO.GetComponent<ModTagDisplay>();
                display.Initialize();
                display.DisplayTag(tagCategory.Key, tagCategory.Value);
                display.onClick += NotifyTagClicked;

                m_tagDisplays.Add(display);
            }

            if(this.isActiveAndEnabled)
            {
                StartCoroutine(LateUpdateLayouting());
            }
        }

        public override void DisplayLoading(int modId = -1)
        {
            m_modId = modId;

            if(loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }

            // clear
            foreach(ModTagDisplay display in m_tagDisplays)
            {
                GameObject.Destroy(display.gameObject);
            }
            m_tagDisplays.Clear();
        }

        // ---------[ EVENTS ]---------
        public void NotifyTagClicked(ModTagDisplay component, string tagName, string category)
        {
            if(tagClicked != null)
            {
                tagClicked(component, tagName, category);
            }
        }
    }
}
