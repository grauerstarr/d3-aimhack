using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Enigma.Wpf;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Media.Media3D;
using Enigma.D3.MapHack.Markers;
using Enigma.D3.MemoryModel;
using Enigma.D3.MemoryModel.Core;
using Enigma.D3.MemoryModel.Caching;
using System.Windows.Forms;
using WindowsInput;


using Enigma.D3.Enums;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Threading;

namespace Enigma.D3.MapHack
{
    internal class Minimap
    {
        private readonly Canvas _window;
        private readonly Canvas _root;
        private readonly MinimapControl _minimapControl;
        private readonly ObservableCollection<IMapMarker> _minimapItems;
        private readonly Dictionary<int, IMapMarker> _minimapItemsDic = new Dictionary<int, IMapMarker>();
        private List<ACD> _moppes = new List<ACD>();
        private int _previousFrame;
        private readonly HashSet<int> _ignoredSnoIds = new HashSet<int>();
        private readonly HashSet<int> _ignoremeSnoIds = new HashSet<int>();

        private ACD _playerAcd;
        private LocalData _localData;
        private ObjectManager _objectManager;
        private ContainerCache<ACD> _acdsObserver;
        private bool _isLocalActorReady;

        


        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);
                
        public Minimap(Canvas overlay)
        {
            if (overlay == null)
                throw new ArgumentNullException(nameof(overlay));

            _minimapItems = new ObservableCollection<IMapMarker>();

            _root = new Canvas() { Height = (int)(PresentationSource.FromVisual(overlay).CompositionTarget.TransformToDevice.M22 * 1200 + 0.5) };
            _window = overlay;
            _window.Children.Add(_root);
            _window.SizeChanged += (s, e) => UpdateSizeAndPosition();

            _root.Children.Add(_minimapControl = new MinimapControl { DataContext = this });

            UpdateSizeAndPosition();
        }

        public ObservableCollection<IMapMarker> MinimapMarkers => _minimapItems;

        private void UpdateSizeAndPosition()
        {
            var uiScale = _window.ActualHeight / 1200d;
            _root.Width = _window.ActualWidth / uiScale;
            _root.RenderTransform = new ScaleTransform(uiScale, uiScale, 0, 0);
        }
        
        public void Update(MemoryContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));
            ctx.Memory.Reader.ResetCounters();
            try
            {
                if (!IsLocalActorValid(ctx))
                    return;

                if (!IsObjectManagerOnNewFrame(ctx))
                    return;

                var itemsToAdd = new List<IMapMarker>();
                var itemsToRemove = new List<IMapMarker>();

                _acdsObserver = _acdsObserver ?? new ContainerCache<ACD>(ctx.DataSegment.ObjectManager.ACDManager.ActorCommonData);
                _acdsObserver.Update();

                // Must have a local ACD to base coords on.
                if (_playerAcd == null)
                {
                    var playerAcdId = ctx.DataSegment.ObjectManager.PlayerDataManager[
                        ctx.DataSegment.ObjectManager.Player.LocalPlayerIndex].ACDID;

                    _playerAcd = _acdsObserver.Items[(short)playerAcdId];
                }


                foreach (var acd in ctx.DataSegment.ObjectManager.ACDManager.ActorCommonData)
                {
                    Console.WriteLine(acd.Name);

                    if (!((acd.MonsterQuality == MonsterQuality.Champion) || (acd.MonsterQuality == MonsterQuality.Rare) || (acd.MonsterQuality == MonsterQuality.Boss)))
                        continue;

                    

                    if (WindowsInput.InputSimulator.IsKeyDown(VirtualKeyCode.TAB))
                    {
                        
                        
                        System.Drawing.Point MouseCords = D3ToScreen.FromD3toScreenCoords(acd.Position, _playerAcd.Position);
                        if ((MouseCords.X > 0 && MouseCords.Y > 0) && (MouseCords.X < System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width && MouseCords.Y < System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height))
                        {
                            SetCursorPos(MouseCords.X, MouseCords.Y);
                            Thread.Sleep(600);

                        }
                        //System.Windows.MessageBox.Show("X " + MouseCords.X);
                        
                    }

                    
                }
           











            }
            catch (Exception exception)
            {
                OnUpdateException(exception);
            }
        }

        private bool IsLocalActorValid(MemoryContext ctx)
        {
            _localData = _localData ?? ctx.DataSegment.LocalData;
            _localData.TakeSnapshot();

            if (_localData.Read<byte>(0) == 0xCD) // structure is being updated, everything is cleared with 0xCD ('-')
            {
                if (!_isLocalActorReady)
                    return false;
            }
            else
            {
                if (!_localData.IsStartUpGame)
                {
                    if (!_isLocalActorReady)
                    {
                        _isLocalActorReady = true;
                        OnLocalActorCreated();
                    }
                }
                else
                {
                    if (_isLocalActorReady)
                    {
                        _isLocalActorReady = false;
                        OnLocalActorDisposed();
                    }
                    return false;
                }
            }
            return true;
        }

        private bool IsObjectManagerOnNewFrame(MemoryContext ctx)
        {
            _objectManager = _objectManager ?? ctx.DataSegment.ObjectManager;

            // Don't do anything unless game updated frame.
            int currentFrame = _objectManager.RenderTick;
            if (currentFrame == _previousFrame)
                return false;

            if (currentFrame < _previousFrame)
            {
                // Lesser frame than before = left game probably.
                Reset();
                return false;
            }
            _previousFrame = currentFrame;
            return true;
        }

        private void OnUpdateException(Exception exception)
        {
            Trace.WriteLine(exception.Message);
            Reset();
        }

        private void UpdateUI(List<IMapMarker> itemsToAdd, List<IMapMarker> itemsToRemove)
        {
            if (itemsToRemove.Count > 0)
            {
                //Trace.WriteLine("Removing " + itemsToRemove.Count + " items...");
                Execute.OnUIThread(() => itemsToRemove.ForEach(x => _minimapItems.Remove(x)));
                itemsToRemove.ForEach(a => _minimapItemsDic.Remove(a.Id));
            }
            
            if (_playerAcd != null)
            {
                var origo = new Point3D(_playerAcd.Position.X, _playerAcd.Position.Y, _playerAcd.Position.Z);
                foreach (var mapItem in _minimapItems.Concat(itemsToAdd))
                    mapItem.Update(_playerAcd.SWorldID, origo);
            }

            if (itemsToAdd.Count > 0)
            {
                //Trace.WriteLine("Adding " + itemsToAdd.Count + " items...");
                Execute.OnUIThread(() => itemsToAdd.ForEach(a => _minimapItems.Add(a)));
            }
        }

        private void OnLocalActorCreated()
        {
            Trace.WriteLine("Local Actor Ready");
        }

        private void OnLocalActorDisposed()
        {
            Trace.WriteLine("Local Actor Not Ready");
            Reset();
        }

        private void Reset()
        {
            _minimapItemsDic.Clear();
            if (_minimapItems.Count > 0)
                Execute.OnUIThread(() => _minimapItems.Clear());
            _acdsObserver = null;
            _playerAcd = null;
            _ignoredSnoIds.Clear();
            _localData = null;
            _objectManager = null;
            _isLocalActorReady = false;
            _previousFrame = 0;
        }
    }
}
