
using System.Collections.Generic;
using UnityEngine;
using BasicDeltaV.Unity.Unity;

namespace BasicDeltaV.Unity
{
    public class BasicDVPanelManager : MonoBehaviour
    {
        List<BasicDeltaV_Panel> _activePanels = new List<BasicDeltaV_Panel>();

        private int _updateCounter;

        private static BasicDVPanelManager _instance;

        public static BasicDVPanelManager Instance
        {
            get { return _instance; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            _activePanels.Clear();
        }

        public void RegisterPanel(BasicDeltaV_Panel panel)
        {
            if (!_activePanels.Contains(panel))
                _activePanels.Add(panel);
        }

        public void UnregisterPanel(BasicDeltaV_Panel panel)
        {
            if (_activePanels.Contains(panel))
                _activePanels.Remove(panel);
        }

        private void Update()
        {
            if (_activePanels == null || _activePanels.Count <= 0)
                return;

            _updateCounter++;

            if (_updateCounter >= _activePanels.Count)
                _updateCounter = 0;

            _activePanels[_updateCounter].OnUpdate();
        }
    }
}
