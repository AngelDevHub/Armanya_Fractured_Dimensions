using System.Collections;
using UnityEngine;

namespace Gamekit2D
{
    public class HealthUI : MonoBehaviour
    {
        public Damageable representedDamageable;
        public GameObject healthIconPrefab;

        protected Animator[] m_HealthIconAnimators;

        protected readonly int m_HashActivePara = Animator.StringToHash ("Active");
        protected readonly int m_HashInactiveState = Animator.StringToHash ("Inactive");
        protected const float k_HeartIconAnchorWidth = 0.041f;

        IEnumerator Start ()
        {
            if (representedDamageable == null)
            {
                var p2d = FindFirstObjectByType<Armanya.Player.PlayerController2D>();
                if (p2d != null) representedDamageable = p2d.GetComponent<Damageable>();
                if (representedDamageable == null)
                {
                    var pc = FindFirstObjectByType<PlayerCharacter>();
                    if (pc != null) representedDamageable = pc.damageable;
                }
            }

            if(representedDamageable == null)
                yield break;

            yield return null;
            BuildUI();
        }

        public void SetDamageable(Damageable dmg)
        {
            representedDamageable = dmg;
            BuildUI();
        }

        public void BuildUI()
        {
            if (representedDamageable == null || healthIconPrefab == null)
                return;

            if (m_HealthIconAnimators != null)
            {
                for (int i = 0; i < m_HealthIconAnimators.Length; i++)
                {
                    if (m_HealthIconAnimators[i] != null)
                        Destroy(m_HealthIconAnimators[i].gameObject);
                }
            }

            m_HealthIconAnimators = new Animator[representedDamageable.startingHealth];

            for (int i = 0; i < representedDamageable.startingHealth; i++)
            {
                GameObject healthIcon = Instantiate (healthIconPrefab);
                healthIcon.transform.SetParent (transform);
                RectTransform healthIconRect = healthIcon.transform as RectTransform;
                healthIconRect.anchoredPosition = Vector2.zero;
                healthIconRect.sizeDelta = Vector2.zero;
                healthIconRect.anchorMin += new Vector2(k_HeartIconAnchorWidth, 0f) * i;
                healthIconRect.anchorMax += new Vector2(k_HeartIconAnchorWidth, 0f) * i;
                m_HealthIconAnimators[i] = healthIcon.GetComponent<Animator> ();

                if (representedDamageable.CurrentHealth < i + 1)
                {
                    m_HealthIconAnimators[i].Play (m_HashInactiveState);
                    m_HealthIconAnimators[i].SetBool (m_HashActivePara, false);
                }
            }
        }

        public void ChangeHitPointUI (Damageable damageable)
        {
            if (damageable != null && (m_HealthIconAnimators == null || m_HealthIconAnimators.Length == 0))
            {
                representedDamageable = damageable;
                BuildUI();
                return;
            }

            if(m_HealthIconAnimators == null)
                return;
            
            for (int i = 0; i < m_HealthIconAnimators.Length; i++)
            {
                if (m_HealthIconAnimators[i] != null)
                    m_HealthIconAnimators[i].SetBool(m_HashActivePara, damageable.CurrentHealth >= i + 1);
            }
        }
    }
}