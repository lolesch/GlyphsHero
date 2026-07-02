using Code.Runtime.Modules.Statistics;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Code.Runtime.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class PawnResourceView : MonoBehaviour
    {
        [SerializeField, ReadOnly] private Resource resource;
        [FormerlySerializedAs("healthbar")] [SerializeField]           private Image    bar;

        private void Awake()
        {
            if (bar == null) { bar = GetComponent<Image>(); Debug.LogWarning("Assign resource bar in Inspector.", this); }
        }
        
        public void SetPawn( Resource res )
        {
            if( res == null )
                return;

            var previous = resource;
            if( previous != null )
                previous.OnCurrentChanged -= UpdateView;

            resource = res;
            resource.OnCurrentChanged += UpdateView;

            // Paint the current value immediately — OnCurrentChanged only fires on later changes,
            // so without this the bar stays at its authored fill until the first damage/regen tick.
            Paint();
        }

        private void OnDestroy()
        {
            if( resource != null )
                resource.OnCurrentChanged -= UpdateView;
        }

        private void UpdateView( float prev, float curr, float max ) => Paint();

        private void Paint()
        {
            if( bar != null && resource != null )
                bar.fillAmount = resource.Percentage;
        }
    }
}