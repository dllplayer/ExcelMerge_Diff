﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows;
using Prism.Mvvm;
using FastWpfGrid;
using ExcelMerge.GUI.Settings;
using ExcelMerge.GUI.Behaviors;

namespace ExcelMerge.GUI.ViewModels
{
    public class DiffViewModel : BindableBase
    {
        private bool showLocationGridLine;
        public bool ShowLocationGridLine
        {
            get { return showLocationGridLine; }
            set { SetProperty(ref showLocationGridLine, value); }
        }

        private string srcPath;
        public string SrcPath
        {
            get { return srcPath; }
            set
            {
                SetProperty(ref srcPath, value);
                Settings.EMEnvironmentValue.Set("SRC", value);
                UpdateExecutableFlag();
            }
        }

        private string dstPath;
        public string DstPath
        {
            get { return dstPath; }
            set
            {
                SetProperty(ref dstPath, value);
                Settings.EMEnvironmentValue.Set("DST", value);
                UpdateExecutableFlag();
            }
        }

        private string defSheetName;
        public string DefSheetName
        {
            get { return defSheetName; }
            set
            {
                SetProperty(ref defSheetName, value);
                UpdateExecutableFlag();
            }
        }

        private List<string> srcSheetNames;
        public List<string> SrcSheetNames
        {
            get { return srcSheetNames; }
            private set { SetProperty(ref srcSheetNames, value); }
        }

        private List<string> dstSheetNames;
        public List<string> DstSheetNames
        {
            get { return dstSheetNames; }
            private set { SetProperty(ref dstSheetNames, value); }
        }

        private int selectedSrcSheetIndex;
        public int SelectedSrcSheetIndex
        {
            get { return selectedSrcSheetIndex; }
            set { SetProperty(ref selectedSrcSheetIndex, value); }
        }

        private int selectedDstSheetIndex;
        public int SelectedDstSheetIndex
        {
            get { return selectedDstSheetIndex; }
            set { SetProperty(ref selectedDstSheetIndex, value); }
        }

        private bool executable;
        public bool Executable
        {
            get { return executable; }
            private set { SetProperty(ref executable, value); }
        }

        private int modifiedCellCount;
        public int ModifiedCellCount
        {
            get { return modifiedCellCount; }
            private set { SetProperty(ref modifiedCellCount, value); }
        }

        private int modifiedRowCount;
        public int ModifiedRowCount
        {
            get { return modifiedRowCount; }
            private set { SetProperty(ref modifiedRowCount, value); }
        }

        private int addedRowCount;
        public int AddedRowCount
        {
            get { return addedRowCount; }
            private set { SetProperty(ref addedRowCount, value); }
        }

        private int removedRowCount;
        public int RemovedRowCount
        {
            get { return removedRowCount; }
            private set { SetProperty(ref removedRowCount, value); }
        }

        private DragAcceptDescription description;
        public DragAcceptDescription Description
        {
            get { return description; }
            private set { SetProperty(ref description, value); }
        }

        public DiffViewModel()
        {
            Description = new DragAcceptDescription();
            Description.DragDrop += DragDrop;
            Description.DragDrop += DragOver;

            SrcPath = string.Empty;
            DstPath = string.Empty;
            DefSheetName = string.Empty;
        }

        public DiffViewModel(string src, string dst, string defSheetName, MainWindowViewModel mwv) : this()
        {
            SrcPath = src;
            DstPath = dst;
            DefSheetName = defSheetName;

            mwv.PropertyChanged += Mwv_PropertyChanged;
        }

        public void UpdateDiffSummary(ExcelSheetDiffSummary summary)
        {
            ModifiedCellCount = summary.ModifiedCellCount;
            ModifiedRowCount = summary.ModifiedRowCount;
            AddedRowCount = summary.AddedRowCount;
            RemovedRowCount = summary.RemovedRowCount;
        }

        private void Mwv_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SrcPath))
            {
                var vm = sender as MainWindowViewModel;
                if (vm != null)
                {
                    var prop = typeof(MainWindowViewModel).GetProperties().FirstOrDefault(p => p.Name == e.PropertyName);
                    if (prop != null)
                    {
                        SrcPath = prop.GetValue(vm) as string;
                    }
                }
            }
            else if (e.PropertyName == nameof(DstPath))
            {
                var vm = sender as MainWindowViewModel;
                if (vm != null)
                {
                    var prop = typeof(MainWindowViewModel).GetProperties().FirstOrDefault(p => p.Name == e.PropertyName);
                    if (prop != null)
                    {
                        DstPath = prop.GetValue(vm) as string;
                    }
                }
            }
        }

        private void DragDrop(DragEventArgs e)
        {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || !paths.Any())
                return;

            var target = e.Source as FrameworkElement;
            if (target == null)
                return;

            OnDragDrop(paths, target);
        }

        protected virtual void OnDragDrop(string[] filePath, FrameworkElement target)
        {
            if (filePath.Length > 1)
            {
                SrcPath = filePath[1];
                DstPath = filePath[0];

                return;
            }

            var tag = Convert.ToInt32(target.Tag);
            if (tag == 0)
                SrcPath = filePath[0];
            else if (tag == 1)
                DstPath = filePath[0];
        }

        private void DragOver(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void UpdateExecutableFlag()
        {
            var existsSrc = File.Exists(SrcPath);
            var existsDst = File.Exists(DstPath);

            if (existsSrc)
            {
                SrcSheetNames = ExcelWorkbook.GetSheetNames(SrcPath).ToList();
                
                if (DefSheetName == string.Empty)
                {
                    SelectedSrcSheetIndex = 0;
                }
                else
                {
                    SelectedSrcSheetIndex = 0;
                    for (int i = 0; i < SrcSheetNames.Count; i++)
                    {
                        if (SrcSheetNames[i] == DefSheetName)
                        {
                            SelectedSrcSheetIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                SrcSheetNames = new List<string>();
                SelectedSrcSheetIndex = -1;
            }

            if (existsDst)
            {
                DstSheetNames = ExcelWorkbook.GetSheetNames(DstPath).ToList();
                
                if (DefSheetName == string.Empty)
                {
                    SelectedDstSheetIndex = 0;
                }
                else
                {
                    SelectedDstSheetIndex = 0;
                    for (int i = 0; i < DstSheetNames.Count; i++)
                    {
                        if (DstSheetNames[i] == DefSheetName)
                        {
                            SelectedDstSheetIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                DstSheetNames = new List<string>();
                SelectedDstSheetIndex = -1;
            }

            Executable = existsSrc && existsDst;
        }
    }
}
