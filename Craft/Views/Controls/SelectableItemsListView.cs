using Craft.Utils;
// <copyright file="SelectableItemsListView.cs">
//     Copyright (c) 2020 Joseph Dwyer All rights reserved.
// </copyright>

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Craft.Views.Controls
{
    public class SelectableItemsListView : ListView
    {
        public event EventHandler<ListViewItem> ItemConfirmed;

        private DateTime lastClicked;
        private object lastSelectedItem;

        static SelectableItemsListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectableItemsListView), new FrameworkPropertyMetadata(typeof(SelectableItemsListView)));
        }

        public SelectableItemsListView()
        {
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            // Set Handled true so that events from parent controls aren't also raised.
            e.Handled = true;
            base.OnSelectionChanged(e);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var listViewItem = new ListViewItem();

            listViewItem.PreviewMouseUp += ListViewItemOnPreviewMouseUp;
            listViewItem.StylusSystemGesture += ListViewItemOnStylusSystemGesture;

            return listViewItem;
        }


        private void ListViewItemOnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (e.StylusDevice == null)
            {
                e.Handled = ItemIsConfirmed((ListViewItem)sender);
            }
            else
            {
                // Since the MouseDoubleClick event doesn't propagate the handled event correctly: https://msdn.microsoft.com/en-us/library/system.windows.controls.control.mousedoubleclick.aspx
                // this is a cheap implementation of a PreviewMouseDoubleClick that will handle the final mouse up event, since this is last event in the double click chain
                DateTime clickedDateTime = DateTime.Now;
                if (clickedDateTime.Subtract(lastClicked).TotalMilliseconds <= Native.GetDoubleClickTime() && ReferenceEquals(sender, lastSelectedItem))
                {
                    e.Handled = ItemIsConfirmed((ListViewItem)sender);
                    lastSelectedItem = null;
                    lastClicked = DateTime.MinValue;
                }
                else
                {
                    lastSelectedItem = sender;
                    lastClicked = clickedDateTime;
                }
            }
        }

        private void ListViewItemOnStylusSystemGesture(object sender, StylusSystemGestureEventArgs e)
        {
            if (e.SystemGesture == SystemGesture.Tap)
            {
                SelectedItem = ((FrameworkElement)sender).DataContext;
                e.Handled = ItemIsConfirmed((ListViewItem)sender);
            }
            else
            {
                e.Handled = false;
            }
        }

        private bool ItemIsConfirmed(ListViewItem listViewItem)
        {
            ItemConfirmed?.Invoke(this, listViewItem);

            return true;
        }
    }
}