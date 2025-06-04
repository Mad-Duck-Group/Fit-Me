using MadDuck.Scripts.Units;
using Redcode.Extensions;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    public class PointerManager : MonoSingleton<PointerManager>
    {
        [SerializeField] private Camera gameCamera;
        private Block _selectedBlock;
        private Vector3 _mousePositionDifference;
        public Vector3 MousePosition
        {
            get
            {
                Vector3 mousePosition = gameCamera.ScreenToWorldPoint(Input.mousePosition).WithZ(0);
                return mousePosition;
            }
        }
    
        protected override void Awake()
        {
            base.Awake();
            gameCamera = Camera.main;
        }
    
        void Update()
        {
            HandleMovement();
        }
    
        /// <summary>
        /// Select a block
        /// </summary>
        /// <param name="block">Block to select</param>
        public void SelectBlock(Block block)
        {
            if (_selectedBlock && _selectedBlock == block) return;
            _selectedBlock = block;
            var position = _selectedBlock.transform.position;
            _mousePositionDifference = new Vector3(MousePosition.x - position.x,
                MousePosition.y - position.y, 0);
            _selectedBlock.SetRendererSortingOrder(2);
        }
    
        /// <summary>
        /// Deselect current block
        /// </summary>
        public void DeselectBlock()
        {
            if (!_selectedBlock) return;
            _mousePositionDifference = Vector3.zero;
            _selectedBlock = null;
        }
    
        /// <summary>
        /// Handle the movement of the selected block
        /// </summary>
        private void HandleMovement()
        {
            if (!_selectedBlock) return;
            _selectedBlock.transform.position = MousePosition - _mousePositionDifference;
        }
    }
}
