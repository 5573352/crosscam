﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Views;
using Android.Widget;
using CustomRenderer.Droid.CustomRenderer;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Camera = Android.Hardware.Camera;
using CameraModule = CustomRenderer.CustomElement.CameraModule;
#pragma warning disable 618

[assembly: ExportRenderer(typeof(CameraModule), typeof(CameraModuleRenderer))]
namespace CustomRenderer.Droid.CustomRenderer
{
    public class CameraModuleRenderer : ViewRenderer<CameraModule, Android.Views.View>, TextureView.ISurfaceTextureListener
    {
        private Camera _camera;
        private Android.Views.View _view;

        private Activity _activity;
        private CameraFacing _cameraType;
        private TextureView _textureView;
        private SurfaceTexture _surfaceTexture;
        private CameraModule _cameraModule;

        private static Camera.Size _previewSize;
        private static Camera.Size _pictureSize;
        
        private bool _isRunning;
        private bool _isSurfaceAvailable;

        public CameraModuleRenderer(Context context) : base(context) {}

        protected override void OnElementChanged(ElementChangedEventArgs<CameraModule> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            if (e.NewElement != null)
            {
                _cameraModule = e.NewElement;
            }
            
            SetupUserInterface();
            AddView(_view);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(_cameraModule.IsVisible))
            {
                if (_camera != null)
                {
                    if (!_cameraModule.IsVisible)
                    {
                        StopCamera();
                    }
                    else
                    {
                        if (_isSurfaceAvailable)
                        {
                            PrepareAndStartCamera();
                        }
                    }
                }
            }

            if (e.PropertyName == nameof(_cameraModule.CaptureTrigger))
            {
                if (_cameraModule.IsVisible)
                {
                    TakePhotoButtonTapped();
                }
            }
        }

        private void SetupUserInterface()
        {
            _activity = Context as Activity;
            _view = _activity.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false);
            _cameraType = CameraFacing.Back;

            _textureView = _view.FindViewById<TextureView>(Resource.Id.textureView);
            _textureView.SurfaceTextureListener = this;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            _view.Measure(msw, msh);
            _view.Layout(0, 0, r - l, b - t);
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _textureView.LayoutParameters = new FrameLayout.LayoutParams(width, height);
            _surfaceTexture = surface;
            _isSurfaceAvailable = true;

            PrepareAndStartCamera(surface);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            _isSurfaceAvailable = false;
            StopCamera();
            return true;
        }

        private void StopCamera()
        {
            if (_isRunning)
            {
                if (_camera != null)
                {
                    _camera.StopPreview();
                    _camera.Release();
                }

                _isRunning = false;
            }
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
        }

        private void PrepareAndStartCamera(SurfaceTexture surface = null)
        {
            _camera = Camera.Open((int) _cameraType);

            if (surface != null)
            {
                _surfaceTexture = surface;
            }

            var parameters = _camera.GetParameters();
            parameters.FlashMode = Camera.Parameters.FlashModeOff;

            if (_pictureSize == null ||
                _previewSize == null)
            {
                var landscapePictureDescendingSizes = parameters
                    .SupportedPictureSizes
                    .Where(p => p.Width > p.Height)
                    .OrderByDescending(p => p.Width * p.Height)
                    .ToList();
                var landscapePreviewDescendingSizes = parameters
                    .SupportedPreviewSizes
                    .Where(p => p.Width > p.Height)
                    .OrderByDescending(p => p.Width * p.Height)
                    .ToList();

                foreach (var pictureSize in landscapePictureDescendingSizes)
                {
                    foreach (var previewSize in landscapePreviewDescendingSizes)
                    {
                        if (Math.Abs((double)pictureSize.Width / pictureSize.Height - (double)previewSize.Width / previewSize.Height) < 0.0001)
                        {
                            _pictureSize = pictureSize;
                            _previewSize = previewSize;
                            break;
                        }
                    }

                    if (_pictureSize != null)
                    {
                        break;
                    }
                }

                if (_pictureSize == null ||
                    _previewSize == null)
                {
                    _pictureSize = landscapePictureDescendingSizes.First();
                    _previewSize = landscapePreviewDescendingSizes.First();
                }
            }

            parameters.SetPictureSize(_pictureSize.Width, _pictureSize.Height);
            parameters.SetPreviewSize(_previewSize.Width, _previewSize.Height);

            _camera.SetParameters(parameters);
            _camera.SetPreviewTexture(_surfaceTexture);

            var display = _activity.WindowManager.DefaultDisplay;
            if (display.Rotation == SurfaceOrientation.Rotation0)
            {
                _camera.SetDisplayOrientation(90);
            }

            if (display.Rotation == SurfaceOrientation.Rotation270)
            {
                _camera.SetDisplayOrientation(180);
            }

            _camera.StartPreview();

            _isRunning = true;
        }

        private async void TakePhotoButtonTapped()
        {
            _camera.StopPreview();

            var image = _textureView.Bitmap;
            var stream = new MemoryStream();
            await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 100, stream);
            _cameraModule.CapturedImage = stream.ToArray();

            _camera.StartPreview();
        }
    }
}

