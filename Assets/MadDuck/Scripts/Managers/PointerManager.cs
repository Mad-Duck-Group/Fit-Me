using System;
using MadDuck.Scripts.Units;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    public class PointerManager : MonoSingleton<PointerManager>
    {
        [Title("Pointer Manager References")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Canvas gameCanvas;

        public Vector3 MouseWorldPosition
        {
            get
            {
                Vector3 mousePosition = gameCamera.ScreenToWorldPoint(Input.mousePosition).WithZ(0);
                return mousePosition;
            }
        }
        
        public Vector3 MouseCanvasPosition
        {
            get
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gameCanvas.transform as RectTransform, 
                    Input.mousePosition, 
                    gameCanvas.worldCamera, 
                    out Vector2 localPoint);
                return gameCanvas.transform.TransformPoint(localPoint);
            }
        }
    
        protected override void Awake()
        {
            base.Awake();
            gameCamera = Camera.main;
        }
    }
}
