
using System.Collections.Generic;
using System.Text;
using BasicDeltaV.Unity.Interface;
using UnityEngine;
using UnityEngine.UI;

namespace BasicDeltaV.Unity.Unity
{
    public class BasicDeltaV_SimplePanel : MonoBehaviour
    {
        [SerializeField]
        private Image m_Background = null;
        [SerializeField]
        private Image m_Header = null;
        [SerializeField]
        private TextHandler m_ModuleText = null;

        private bool rightPos = false;
        private bool shifted = false;
        
        private bool noDVMode = false;

        private RectTransform rect;
        private CanvasGroup cg;
        private List<IBasicModule> Modules = new List<IBasicModule>();

        private StringBuilder panelString = new StringBuilder(164);

        public bool RightPos
        {
            get { return rightPos; }
        }

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            cg = GetComponent<CanvasGroup>();
        }

        public void OnHide()
        {
            if (cg != null)
                cg.alpha = 0.2f;
        }

        public void OnUnHide()
        {
            if (cg != null)
                cg.alpha = 1;
        }
        
        public void SetNoDVMode(bool isOn)
        {
            noDVMode = isOn;
        }

        public void Unregister()
        {
            BasicDVPanelManager.Instance.UnregisterPanel(this);
        }

        public void Register()
        {
            BasicDVPanelManager.Instance.RegisterPanel(this);
        }

        public void Close()
        {
            BasicDVPanelManager.Instance.UnregisterPanel(this);

            gameObject.SetActive(false);

            Destroy(gameObject);
        }

        public void setPanel(List<IBasicModule> modules, float alpha, bool right)
        {
            CreateModules(modules);

            SetAlpha(alpha);

            if (right)
            {
                if (rect != null)
                {
                    rect.pivot = new Vector2(0, 0);

                    rect.anchoredPosition = new Vector2(24, rect.anchoredPosition.y);
                }
            }

            OnUpdate();

            BasicDVPanelManager.Instance.RegisterPanel(this);
        }

        public void MovePanel(bool right)
        {
            int value = 0;

            if (right && !rightPos)
                value = 75;
            else if (!right && rightPos)
                value = -75;

            if (rect != null)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + value, rect.anchoredPosition.y);

            rightPos = right;
        }

        public void MovePanelUp()
        {
            if (rect != null && !shifted)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + 12);

            shifted = true;
        }

        public void SetAlpha(float a)
        {
            if (m_Background != null)
            {
                Color c = m_Background.color;

                c.a = a;

                m_Background.color = c;
            }

            if (m_Header != null)
            {
                Color c = m_Header.color;

                c.a = a;

                m_Header.color = c;
            }
        }

        private void CreateModules(List<IBasicModule> modules)
        {
            if (modules == null || modules.Count == 0)
                return;

            Modules = CalculateOrder(modules);
        }

        private List<IBasicModule> CalculateOrder(List<IBasicModule> modules)
        {
            List<IBasicModule> smallMods = new List<IBasicModule>();
            List<IBasicModule> largeMods = new List<IBasicModule>();

            for (int i = modules.Count - 1; i >= 0; i--)
            {
                IBasicModule mod = modules[i];
                
                if (noDVMode && mod.DVModule)
                    continue;

                if (mod.SmallSize)
                    smallMods.Add(mod);
                else
                    largeMods.Add(mod);
            }

            smallMods.Sort((a, b) => a.FixedOrder.CompareTo(b.FixedOrder));
            largeMods.Sort((a, b) => a.FixedOrder.CompareTo(b.FixedOrder));

            for (int i = 0; i < smallMods.Count; i++)
                smallMods[i].Order = i + 1;

            for (int i = 0; i < largeMods.Count; i++)
                largeMods[i].Order = i + 1;

            if (largeMods.Count == 0)
            {
                for (int i = 0; i < smallMods.Count; i++)
                {
                    smallMods[i].Order = i + 1;

                    if (smallMods[i].Order == 2)
                        smallMods[i].LineBreak = true;
                    else
                        smallMods[i].LineBreak = false;
                }
            }
            else if (largeMods.Count == 1)
            {
                largeMods[0].Order = 2;

                if (smallMods.Count == 1)
                {
                    smallMods[0].Order = 3;

                    smallMods[0].LineBreak = false;
                    largeMods[0].LineBreak = true;
                }
                else if (smallMods.Count == 2)
                {
                    smallMods[0].Order = 3;
                    smallMods[1].Order = 4;

                    smallMods[0].LineBreak = false;
                    smallMods[1].LineBreak = false;
                    largeMods[0].LineBreak = true;
                }
                else
                {
                    for (int i = 0; i < smallMods.Count; i++)
                    {
                        smallMods[i].Order = i <= 0 ? 1 : i + 2;

                        smallMods[i].LineBreak = false;
                    }

                    largeMods[0].LineBreak = true;
                }
            }
            else if (largeMods.Count == 2)
            {
                largeMods[0].Order = 2;
                largeMods[1].Order = 4;

                if (smallMods.Count == 1)
                {
                    smallMods[0].Order = 3;

                    smallMods[0].LineBreak = false;

                    largeMods[0].LineBreak = true;
                    largeMods[1].LineBreak = false;
                }
                else if (smallMods.Count == 2)
                {
                    smallMods[0].Order = 1;
                    smallMods[1].Order = 3;

                    smallMods[0].LineBreak = false;
                    smallMods[1].LineBreak = false;

                    largeMods[0].LineBreak = true;
                    largeMods[1].LineBreak = false;
                }
                else
                {
                    for (int i = 0; i < smallMods.Count; i++)
                    {
                        smallMods[i].Order = (i * 2) + 1;
                        
                        smallMods[i].LineBreak = false;
                    }

                    largeMods[0].LineBreak = true;
                    largeMods[1].LineBreak = true;
                }
            }
            else
            {
                for (int i = 0; i < largeMods.Count; i++)
                {
                    largeMods[i].Order = (i + 1) * 2;

                    if (largeMods[i].Order == 6)
                        largeMods[i].LineBreak = false;
                    else
                        largeMods[i].LineBreak = true;
                }

                for (int i = 0; i < smallMods.Count; i++)
                {
                    smallMods[i].Order = (i * 2) + 1;

                    smallMods[i].LineBreak = false;
                }
            }

            List<IBasicModule> orderedMods = new List<IBasicModule>();

            orderedMods.AddRange(smallMods);
            orderedMods.AddRange(largeMods);

            orderedMods.Sort((a, b) => a.Order.CompareTo(b.Order));

            return orderedMods;
        }

        public void OnUpdate()
        {
            panelString.Length = 0;

            for (int i = 0; i < Modules.Count; i++)
            {
                IBasicModule mod = Modules[i];

                if (mod == null)
                    continue;
                
                mod.Update(panelString);

                if (mod.LineBreak)
                    panelString.Append("\n");
                else if (i != Modules.Count - 1)
                    panelString.Append(" ");
            }

            m_ModuleText.OnTextUpdate.Invoke(panelString.ToString());
        }
    }
}
