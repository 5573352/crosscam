﻿using System;
using System.IO;
using System.Threading.Tasks;
using CrossCam.Model;
using CrossCam.Wrappers;
using FreshMvvm;
using SkiaSharp;
using Xamarin.Forms;

namespace CrossCam.ViewModel
{
    public sealed class CameraViewModel : FreshBasePageModel
    {
        public ImageSource LeftImageSource { get; set; }
        public byte[] LeftByteArray { get; set; }
        public bool IsLeftCameraVisible { get; set; }
        public Command RetakeLeftCommand { get; set; }
        public bool LeftCaptureSuccess { get; set; }

        public ImageSource RightImageSource { get; set; }
        public byte[] RightByteArray { get; set; }
        public bool IsRightCameraVisible { get; set; }
        public Command RetakeRightCommand { get; set; }
        public bool RightCaptureSuccess { get; set; }

        public Command CapturePictureCommand { get; set; }
        public bool CapturePictureTrigger { get; set; }
        
        public Command SaveCapturesCommand { get; set; }

        public Command ToggleViewModeCommand { get; set; }
        public bool IsViewMode { get; set; }

        public Command ClearCapturesCommand { get; set; }

        public Command NavigateToSettingsCommand { get; set; }

        public Settings Settings { get; set; }

        public bool IsViewPortrait { get; set; }
        public bool WasLeftCapturePortrait { get; set; }

        public bool FailFadeTrigger { get; set; }
        public bool SuccessFadeTrigger { get; set; }
        public bool IsSaving { get; set; }

        public Aspect PreviewAspect => Settings.FillScreenPreview && !(IsViewMode && IsViewPortrait) ? Aspect.AspectFill : Aspect.AspectFit;

        public bool IsCaptureComplete => LeftByteArray != null && RightByteArray != null;
        public bool IsNothingCaptured => LeftByteArray == null && RightByteArray == null;
        public bool ShouldHelpTextBeVisible => IsNothingCaptured && !IsSaving && !IsViewMode;
        public bool ShouldLeftRetakeBeVisible => LeftByteArray != null && !IsSaving && !IsViewMode;
        public bool ShouldRightRetakeBeVisible => RightByteArray != null && !IsSaving && !IsViewMode;
        public bool ShouldEndButtonsBeVisible => IsCaptureComplete && !IsSaving && !IsViewMode;
        public bool ShouldSettingsBeVisible => IsNothingCaptured && !IsSaving && !IsViewMode;
        public bool ShouldLineGuidesBeVisible => !IsCaptureComplete && Settings.AreGuideLinesVisible;
        public bool ShouldDonutGuideBeVisible => !IsCaptureComplete && Settings.IsGuideDonutVisible;
        public bool ShouldPortraitWarningBeVisible => ShouldHelpTextBeVisible && IsViewPortrait;

        public string HelpText => "1) Frame up your subject in the center of the screen" + 
                                  "\n2) Drag the horizontal guide lines over some recognizable features of the subject" +
                                  "\n3) Take the left picture (but finish reading these directions first)" +
                                  "\n4) A preview for the right picture will take the place of this text => start cross viewing" + 
                                  "\n5) While keeping the subject centered on the screen and the horizontal guide lines over the same features on the right as they are on the left, begin moving left" +
                                  "\n6) Take the right picture when the desired level of 3D is achieved";

        public CameraViewModel()
        {
            var photoSaver = DependencyService.Get<IPhotoSaver>();
            IsLeftCameraVisible = true;

            Settings = PersistentStorage.LoadOrDefault(PersistentStorage.SETTINGS_KEY, new Settings());

            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(LeftByteArray) &&
                    LeftByteArray != null)
                {
                    LeftImageSource = ImageSource.FromStream(() => new MemoryStream(LeftByteArray));
                    IsLeftCameraVisible = false;
                    WasLeftCapturePortrait = IsViewPortrait;
                    if (RightByteArray == null)
                    {
                        IsRightCameraVisible = true;
                    }
                }
                else if (args.PropertyName == nameof(RightByteArray) &&
                         RightByteArray != null)
                {
                    RightImageSource = ImageSource.FromStream(() => new MemoryStream(RightByteArray));
                    IsRightCameraVisible = false;
                }
            };

            RetakeLeftCommand = new Command(() =>
            {
                IsRightCameraVisible = false;
                IsLeftCameraVisible = true;
                LeftByteArray = null;
                LeftImageSource = null;
            });

            RetakeRightCommand = new Command(() =>
            {
                if (!IsLeftCameraVisible)
                {
                    IsRightCameraVisible = true;
                    RightByteArray = null;
                    RightImageSource = null;
                }
            });

            ClearCapturesCommand = new Command(ClearCaptures);

            CapturePictureCommand = new Command(() =>
            {
                CapturePictureTrigger = !CapturePictureTrigger;
            });

            ToggleViewModeCommand = new Command(() =>
            {
                IsViewMode = !IsViewMode;
            });

            NavigateToSettingsCommand = new Command(async () =>
            {
                await CoreMethods.PushPageModel<SettingsViewModel>(Settings);
            });

            SaveCapturesCommand = new Command(async () =>
            {
                IsSaving = true;
                LeftImageSource = null;
                RightImageSource = null;

                await Task.Delay(500); // breathing room for screen to update

                SKBitmap leftBitmap = null;
                SKBitmap rightBitmap = null;
                SKImage finalImage = null;
                try
                {
                    if (WasLeftCapturePortrait &&
                        IsViewPortrait &&
                        Device.RuntimePlatform == Device.iOS)
                    {
                        leftBitmap = BitmapRotate90(SKBitmap.Decode(LeftByteArray));
                        LeftByteArray = null;

                        rightBitmap = BitmapRotate90(SKBitmap.Decode(RightByteArray));
                        RightByteArray = null;
                    }
                    else if (WasLeftCapturePortrait &&
                             !IsViewPortrait &&
                             Device.RuntimePlatform == Device.iOS)
                    {
                        leftBitmap = BitmapRotateNegative90(SKBitmap.Decode(LeftByteArray));
                        LeftByteArray = null;

                        rightBitmap = BitmapRotateNegative90(SKBitmap.Decode(RightByteArray));
                        RightByteArray = null;
                    }
                    else
                    {
                        leftBitmap = SKBitmap.Decode(LeftByteArray);
                        LeftByteArray = null;

                        rightBitmap = SKBitmap.Decode(RightByteArray);
                        RightByteArray = null;
                    }

                    double eachSideWidth;
                    if (WasLeftCapturePortrait || !Settings.ClipLandscapeToFilledScreenPreview)
                    {
                        eachSideWidth = leftBitmap.Width;
                    }
                    else
                    {
                        var pictureHeightToScreenHeightRatio = leftBitmap.Height / Application.Current.MainPage.Height;
                        eachSideWidth = Application.Current.MainPage.Width * pictureHeightToScreenHeightRatio / 2d;
                    }

                    var imageLeftTrimWidth = (leftBitmap.Width - eachSideWidth) / 2d;

                    var finalImageWidth = eachSideWidth * 2;

                    using (var tempSurface = SKSurface.Create(new SKImageInfo((int)finalImageWidth, leftBitmap.Height)))
                    {
                        var canvas = tempSurface.Canvas;
                        
                        canvas.Clear(SKColors.Transparent);

                        var floatedTrim = (float)imageLeftTrimWidth;
                        var floatedWidth = (float)eachSideWidth;

                        canvas.DrawBitmap(leftBitmap,
                            SKRect.Create(floatedTrim, 0, floatedWidth, leftBitmap.Height),
                            SKRect.Create(0, 0, floatedWidth, leftBitmap.Height));
                        canvas.DrawBitmap(rightBitmap,
                            SKRect.Create(floatedTrim, 0, floatedWidth, leftBitmap.Height),
                            SKRect.Create(floatedWidth, 0, floatedWidth, leftBitmap.Height));

                        finalImage = tempSurface.Snapshot();
                    }

                    byte[] finalImageByteArray;
                    using (var encoded = finalImage.Encode(SKEncodedImageFormat.Jpeg, 100))
                    {
                        finalImageByteArray = encoded.ToArray();
                    }

                    finalImage.Dispose();
                    leftBitmap.Dispose();
                    rightBitmap.Dispose();

                    var didSave = await photoSaver.SavePhoto(finalImageByteArray);
                    IsSaving = false;

                    if (didSave)
                    {
                        SuccessFadeTrigger = !SuccessFadeTrigger;
                    }
                    else
                    {
                        FailFadeTrigger = !FailFadeTrigger;
                    }
                }
                catch
                {
                    finalImage?.Dispose();
                    leftBitmap?.Dispose();
                    rightBitmap?.Dispose();
                }

                ClearCaptures();
            });
        }

        private static SKBitmap BitmapRotate90(SKBitmap originalBitmap)
        {
            var rotated = new SKBitmap(originalBitmap.Height, originalBitmap.Width);

            using (var surface = new SKCanvas(rotated))
            {
                surface.Translate(rotated.Width, 0);
                surface.RotateDegrees(90);
                surface.DrawBitmap(originalBitmap, 0, 0);
            }

            return rotated;
        }

        private static SKBitmap BitmapRotateNegative90(SKBitmap originalBitmap)
        {
            var rotated = new SKBitmap(originalBitmap.Height, originalBitmap.Width);

            using (var surface = new SKCanvas(rotated))
            {
                surface.Translate(0, rotated.Height);
                surface.RotateDegrees(-90);
                surface.DrawBitmap(originalBitmap, 0, 0);
            }

            return rotated;
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);
            RaisePropertyChanged(nameof(ShouldLineGuidesBeVisible)); //TODO: figure out how to have Fody do this
            RaisePropertyChanged(nameof(ShouldDonutGuideBeVisible));
            RaisePropertyChanged(nameof(PreviewAspect));
        }

        private void ClearCaptures()
        {
            LeftByteArray = null;
            RightByteArray = null;
            LeftImageSource = null;
            RightImageSource = null;
            IsRightCameraVisible = false;
            IsLeftCameraVisible = true;
            WasLeftCapturePortrait = false;
        }
    }
}