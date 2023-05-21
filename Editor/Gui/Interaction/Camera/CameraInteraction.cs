using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Interaction.Camera
{
    /// <summary>
    /// Controls the manipulation of camera view within an 3d output window.
    /// Each output window has its own instance. 
    /// </summary>
    public class CameraInteraction
    {
        internal static ICameraManipulator[] ManipulationDevices = Array.Empty<ICameraManipulator>();
        public void Update(ICamera camera, bool allowCameraInteraction)
        {
            if (camera == null)
                return;

            var cameraNodeModified = !_smoothedSetup.Matches(camera) && !_intendedSetup.Matches(camera);
            if (cameraNodeModified)
            {
                _intendedSetup.SetTo(camera);
                _smoothedSetup.SetTo(camera);
                return;
            }

            _viewAxis.ComputeForCamera(camera);
            _deltaTime = ImGui.GetIO().DeltaTime;

            var cameraSwitched = camera != _lastCameraNode;
            if (cameraSwitched)
            {
                _intendedSetup.SetTo(camera);
                _smoothedSetup.SetTo(camera);
                _moveVelocity = Vector3.Zero;
                _lastCameraNode = camera;
            }

            if (allowCameraInteraction && ImGui.IsWindowHovered())
            {
                ManipulateCameraByMouse();
                if (ImGui.IsWindowFocused())
                    ManipulateCameraByKeyboard();
            }

            var frameTime = ImGui.GetTime();

            foreach (var device in ManipulationDevices)
            {
                device.ManipulateCamera(_intendedSetup, frameTime);
            }

            var updateRequired = ComputeSmoothMovement();
            if (!updateRequired)
                return;

            camera.CameraPosition = _smoothedSetup.Position;
            camera.CameraTarget = _smoothedSetup.Target;
        }

        public void ResetView()
        {
            _moveVelocity = Vector3.Zero;
            _intendedSetup.Reset();
        }

        private bool ComputeSmoothMovement()
        {
            var stillDamping = !_smoothedSetup.MatchesSetup(_intendedSetup);
            var stillSliding = _moveVelocity.Length() > CameraInteractionParameters.StopDistanceThreshold;
            var stillOrbiting = _orbitVelocity.Length() > 0.001f;

            var cameraIsStillMoving = stillDamping
                                      || stillSliding
                                      || stillOrbiting
                                      || _manipulatedByMouseWheel
                                      || _manipulatedByKeyboard;
            if (!cameraIsStillMoving)
            {
                _smoothedSetup.SetTo(_intendedSetup);
                _orbitVelocity = Vector2.Zero;
                _moveVelocity = Vector3.Zero;
                return false;
            }

            if (_orbitVelocity.Length() > 0.001f)
            {
                ApplyOrbitVelocity(_orbitVelocity);
                _orbitVelocity = Vector2.Lerp(_orbitVelocity, Vector2.Zero, _deltaTime * CameraInteractionParameters.OrbitHorizontalDamping * 60);
            }

            var maxVelocityForScale = _viewAxis.ViewDistance.Length() * CameraInteractionParameters.MaxMoveVelocity * UserSettings.Config.CameraSpeed;
            if (_moveVelocity.Length() > maxVelocityForScale)
            {
                _moveVelocity *= maxVelocityForScale / _moveVelocity.Length();
            }

            if (!_manipulatedByKeyboard)
            {
                _moveVelocity = Vector3.Lerp(_moveVelocity, Vector3.Zero, _deltaTime * CameraInteractionParameters.CameraMoveDamping * 60);
            }

            _intendedSetup.Position += _moveVelocity * _deltaTime;
            _intendedSetup.Target += _moveVelocity * _deltaTime;

            _smoothedSetup.BlendTo(_intendedSetup, CameraInteractionParameters.CameraDamping, _deltaTime);

            _manipulatedByMouseWheel = false;
            _manipulatedByKeyboard = false;
            return true;
        }

        private void ManipulateCameraByMouse()
        {
            HandleMouseWheel();
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (ImGui.GetIO().KeyAlt)
                {
                    Pan();
                }
                else if (ImGui.GetIO().KeyCtrl)
                {
                    LookAround();
                }
                else
                {
                    var delta = new Vector2(ImGui.GetIO().MouseDelta.X,
                                            -ImGui.GetIO().MouseDelta.Y);
                    _orbitVelocity += delta * _deltaTime * -0.1f;
                }
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                Pan();
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
            {
                LookAround();
            }
        }

        private void HandleMouseWheel()
        {
            var delta = ImGui.GetIO().MouseWheel;
            if (Math.Abs(delta) < 0.01f)
                return;

            var viewDistance = _intendedSetup.Position - _intendedSetup.Target;
            var zoomFactorForCurrentFramerate = 1 + (CameraInteractionParameters.ZoomSpeed * FrameDurationFactor);

            if (UserSettings.Config.AdjustCameraSpeedWithMouseWheel)
            {
                if (
                    ImGui.IsMouseDown(ImGuiMouseButton.Left)
                    || ImGui.IsMouseDown(ImGuiMouseButton.Middle)
                    || ImGui.IsMouseDown(ImGuiMouseButton.Right))
                {
                    if (delta < 0)
                    {
                        //viewDistance *= zoomFactorForCurrentFramerate;
                        UserSettings.Config.CameraSpeed /= zoomFactorForCurrentFramerate;
                    }

                    if (delta > 0)
                    {
                        //viewDistance /= zoomFactorForCurrentFramerate;
                        UserSettings.Config.CameraSpeed *= zoomFactorForCurrentFramerate;
                    }

                    _intendedSetup.Position = _intendedSetup.Target + viewDistance;
                    _manipulatedByMouseWheel = true;
                }
                else
                {
                    //var viewDirLength = _viewAxis.ViewDistance.Length();
                    var acc = CameraInteractionParameters.CameraAcceleration * UserSettings.Config.CameraSpeed * _deltaTime * 60;
                    var sign = delta < 0 ? -1 : 1;
                    _moveVelocity += Vector3.Normalize(_viewAxis.ViewDistance) * acc * sign;
                    _manipulatedByKeyboard = true;
                }
            }
            else
            {
                if (delta < 0)
                {
                    viewDistance *= zoomFactorForCurrentFramerate;

                }

                if (delta > 0)
                {
                    viewDistance /= zoomFactorForCurrentFramerate;

                }

                _intendedSetup.Position = _intendedSetup.Target + viewDistance;
                _manipulatedByMouseWheel = true;
            }
        }

        private void LookAround()
        {
            if (!ImGui.IsWindowHovered())
                return;

            var dragDelta = ImGui.GetIO().MouseDelta;
            var factorX = -dragDelta.X * CameraInteractionParameters.RotateMouseSensitivity * 1.5f;
            var factorY = dragDelta.Y * CameraInteractionParameters.RotateMouseSensitivity * 1.5f;
            var rotAroundX = Matrix4x4.CreateFromAxisAngle(_viewAxis.Left, factorY);
            var rotAroundY = Matrix4x4.CreateFromAxisAngle(_viewAxis.Up, factorX);
            var rot = Matrix4x4.Multiply(rotAroundX, rotAroundY);

            var viewDir2 = new Vector4(_intendedSetup.Target - _intendedSetup.Position, 1);
            var viewDirRotated = Vector4.Transform(viewDir2, rot);
            viewDirRotated = Vector4.Normalize(viewDirRotated);

            var newTarget = _intendedSetup.Position + new Vector3(viewDirRotated.X, viewDirRotated.Y, viewDirRotated.Z);
            _intendedSetup.Target = newTarget;
        }

        private void ApplyOrbitVelocity(Vector2 orbitVelocity)
        {
            if (UserSettings.Config.AdjustCameraSpeedWithMouseWheel)
            {
                var rotAroundX = Matrix4x4.CreateFromAxisAngle(_viewAxis.Left, orbitVelocity.Y);
                var rotAroundY = Matrix4x4.CreateFromAxisAngle(_viewAxis.Up, orbitVelocity.X);
                var rot = Matrix4x4.Multiply(rotAroundX, rotAroundY);

                // The following was an attempt to offset rotation target with cameraSpeed.
                // This approach had too many side effects. I'm leaving it here for reference...
                //
                // var view = _intendedSetup.Target - _intendedSetup.Position;
                // var viewLength = view.Length();
                // var viewDirection = view / viewLength;
                //
                // var rotatedViewDir = Vector3.Transform(viewDirection, rot);
                //
                // var tempTarget = _intendedSetup.Position + viewDirection * UserSettings.Config.CameraSpeed * 3;
                // _intendedSetup.Position = tempTarget - rotatedViewDir  * UserSettings.Config.CameraSpeed * 3;
                // _intendedSetup.Target = _intendedSetup.Position + rotatedViewDir * viewLength;

                var viewDir = _intendedSetup.Target - _intendedSetup.Position;
                var viewDirLength = viewDir.Length();
                viewDir /= viewDirLength;

                var newViewDir = Vector3.Transform(viewDir, rot);
                var newViewVector = newViewDir * viewDirLength;
                _intendedSetup.Position = _intendedSetup.Target - newViewVector;

                _intendedSetup.Target = _intendedSetup.Position + newViewDir * UserSettings.Config.CameraSpeed * DefaultCameraDistance;
            }
            else
            {
                var viewDir = new Vector4(_intendedSetup.Target - _intendedSetup.Position, 1);
                var viewDirLength = viewDir.Length();
                viewDir /= viewDirLength;

                var rotAroundX = Matrix4x4.CreateFromAxisAngle(_viewAxis.Left, orbitVelocity.Y);
                var rotAroundY = Matrix4x4.CreateFromAxisAngle(_viewAxis.Up, orbitVelocity.X);
                var rot = Matrix4x4.Multiply(rotAroundX, rotAroundY);

                var newViewDir = Vector4.Transform(viewDir, rot);
                newViewDir = Vector4.Normalize(newViewDir);
                _intendedSetup.Position = _intendedSetup.Target - new Vector3(newViewDir.X, newViewDir.Y, newViewDir.Z) * viewDirLength;

            }
        }

        private const float DefaultCamFovDegrees = 45;
        public readonly float DefaultCameraDistance = 1f / MathF.Tan(DefaultCamFovDegrees * MathF.PI / 360f);

        private void Pan()
        {
            if (!ImGui.IsWindowHovered())
                return;

            var dragDelta = ImGui.GetIO().MouseDelta;
            var factorX = dragDelta.X / CameraInteractionParameters.RenderWindowHeight;
            var factorY = dragDelta.Y / CameraInteractionParameters.RenderWindowHeight;

            var length = (_intendedSetup.Target - _intendedSetup.Position).Length() * UserSettings.Config.CameraSpeed;
            var delta = _viewAxis.Left * factorX * length
                        + _viewAxis.Up * factorY * length;

            _intendedSetup.Position += delta;
            _intendedSetup.Target += delta;
        }

        private void ManipulateCameraByKeyboard()
        {
            if (!ImGui.IsWindowHovered() || ImGui.GetIO().KeyCtrl)
                return;

            var acc = CameraInteractionParameters.CameraAcceleration * UserSettings.Config.CameraSpeed * _deltaTime * 60;

            if (ImGui.IsKeyDown((ImGuiKey)Key.A) || ImGui.IsKeyDown((ImGuiKey)Key.CursorLeft))
            {
                _moveVelocity += _viewAxis.Left * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((ImGuiKey)Key.D) || ImGui.IsKeyDown((ImGuiKey)Key.CursorRight))
            {
                _moveVelocity -= _viewAxis.Left * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((ImGuiKey)Key.W) || ImGui.IsKeyDown((ImGuiKey)Key.CursorUp))
            {
                _moveVelocity += Vector3.Normalize(_viewAxis.ViewDistance) * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((ImGuiKey)Key.S) || ImGui.IsKeyDown((ImGuiKey)Key.CursorDown))
            {
                _moveVelocity -= Vector3.Normalize(_viewAxis.ViewDistance) * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((ImGuiKey)Key.E))
            {
                _moveVelocity += _viewAxis.Up * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((ImGuiKey)Key.Q))
            {
                _moveVelocity -= _viewAxis.Up * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((ImGuiKey)Key.F))
            {
                _moveVelocity = Vector3.Zero;
                _intendedSetup.Reset();
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((ImGuiKey)Key.C))
            {
                _moveVelocity = Vector3.Zero;
                //_intendedSetup.Reset();
                _intendedSetup.Target = TransformGizmoHandling.GetLatestSelectionCenter();
                _manipulatedByKeyboard = true;
            }
        }

        private struct ViewAxis
        {
            public Vector3 Up;
            public Vector3 Left;
            public Vector3 ViewDistance;

            public void ComputeForCamera(ICamera camera)
            {
                ViewDistance = camera.CameraTarget - camera.CameraPosition;

                var worldUp = Vector3.UnitY;
                var rolledUp = Vector3.Normalize(Vector3.Transform(worldUp, Matrix4x4.CreateFromAxisAngle(ViewDistance, camera.CameraRoll)));

                Left = Vector3.Normalize(Vector3.Cross(rolledUp, ViewDistance));
                Up = Vector3.Normalize(Vector3.Cross(ViewDistance, Left));
            }
        }

        public void ResetCamera(ICamera cam)
        {
            cam.CameraPosition = new Vector3(0, 0, CameraInteractionParameters.DefaultCameraPositionZ);
            cam.CameraTarget = Vector3.Zero;
            cam.CameraRoll = 0;
        }

        private static ViewAxis _viewAxis = new ViewAxis();
        private readonly CameraSetup _smoothedSetup = new CameraSetup();
        private readonly CameraSetup _intendedSetup = new CameraSetup();

        private static float FrameDurationFactor => (ImGui.GetIO().DeltaTime);
        private bool _manipulatedByMouseWheel;
        private bool _manipulatedByKeyboard;

        private Vector3 _moveVelocity;
        private Vector2 _orbitVelocity;
        private ICamera _lastCameraNode;
        private static float _deltaTime;
    }
}