using System.Collections.Generic;
using Characters;
using Items;
using Logic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TradeDetailPanel : FocusableObject
    {
        /// <summary>
        /// The text that goes above the source items panel
        /// </summary>
        public Text SourceText;

        /// <summary>
        /// The source items panel
        /// </summary>
        public Transform TradeSourcePanel;

        /// <summary>
        /// The text that goes above the destination items panel
        /// </summary>
        public Text DestinationText;

        /// <summary>
        /// The destination items panel
        /// </summary>
        public Transform TradeDestinationPanel;

        private int _sourceItemsIndex = 0;
        public int SourceItemsIndex => _sourceItemsIndex;

        /// <summary>
        /// The menu items in the source panel
        /// </summary>
        public List<TradeMenuItem> TradeSourceMenuItems { get; } = new List<TradeMenuItem>();

        private int _destinationItemsIndex = 0;
        public int DestinationItemsIndex => _destinationItemsIndex;

        /// <summary>
        /// The menu items in the destination panel
        /// </summary>
        public List<TradeMenuItem> TradeDestinationMenuItems { get; } = new List<TradeMenuItem>();

        public Character SourceCharacter { get; private set; }
        public Character DestinationCharacter { get; private set; }

        /// <summary>
        /// The valid sides that the cursor can be
        /// </summary>
        public enum Side
        {
            SOURCE,
            DESTINATION
        }

        /// <summary>
        /// Which side the cursor is currently on
        /// </summary>
        public Side CurrentSide { get; set; } = Side.SOURCE;


        public override void OnArrow(float horizontal, float vertical)
        {
            // Moving left or right
            if (Mathf.Abs(horizontal) > Mathf.Abs(vertical))
            {
                int sign = System.Math.Sign(horizontal);
                // Moving from left to right
                if (CurrentSide == Side.SOURCE && TradeDestinationMenuItems.Count > 0 && sign == 1)
                {
                    TradeMenuItem previousItem = TradeSourceMenuItems[SourceItemsIndex];
                    if (previousItem != null)
                    {
                        previousItem.Text.text = previousItem.Text.text.Replace(Menu.INDICATOR, "");
                    }

                    SetItemsTextsIndex(TradeDestinationMenuItems, ref _destinationItemsIndex);
                    CurrentSide = Side.DESTINATION;
                }

                // moving from right to left
                else if (CurrentSide == Side.DESTINATION && TradeSourceMenuItems.Count > 0 && sign == -1)
                {
                    TradeMenuItem previousItem = TradeDestinationMenuItems[DestinationItemsIndex];
                    if (previousItem != null)
                    {
                        previousItem.Text.text = previousItem.Text.text.Replace(Menu.INDICATOR, "");
                    }

                    SetItemsTextsIndex(TradeSourceMenuItems, ref _sourceItemsIndex);
                    CurrentSide = Side.SOURCE;
                }
                else
                {
                    Debug.Log("Just stay where you are I guess");
                }
            }
            // Moving up or down
            else
            {
                int sign = System.Math.Sign(vertical) * -1;
                if (CurrentSide == Side.SOURCE)
                {
                    SetItemsTextsIndex(TradeSourceMenuItems, ref _sourceItemsIndex, (SourceItemsIndex + TradeSourceMenuItems.Count + sign) % TradeSourceMenuItems.Count);
                }
                else
                {
                    SetItemsTextsIndex(TradeDestinationMenuItems, ref _destinationItemsIndex, (DestinationItemsIndex + TradeDestinationMenuItems.Count + sign) % TradeDestinationMenuItems.Count);
                }
            }
        }

        /// <summary>
        /// Show the trade detail panel between two characters
        /// </summary>
        /// <param name="sourceCharacter"></param>
        /// <param name="destinationCharacter"></param>
        public void Show(Character sourceCharacter, Character destinationCharacter)
        {
            SourceCharacter = sourceCharacter;
            SourceText.text = sourceCharacter.CharacterName;
            Debug.LogFormat("Source character: {0} with {1} items", sourceCharacter, sourceCharacter.Items.Count);

            DestinationCharacter = destinationCharacter;
            DestinationText.text = destinationCharacter.CharacterName;
            Debug.LogFormat("Destination character: {0} with {1} items", destinationCharacter, destinationCharacter.Items.Count);

            foreach (Item item in sourceCharacter.Items)
            {
                Debug.LogFormat("Adding source item {0}", item);
                TradeSourceMenuItems.Add(new TradeMenuItem(TradeSourcePanel, item));
            }

            foreach (Item item in destinationCharacter.Items)
            {
                Debug.LogFormat("Adding destination item {0}", item);
                TradeDestinationMenuItems.Add(new TradeMenuItem(TradeDestinationPanel, item));
            }

            if (SourceCharacter.Items.Count > 0)
            {
                CurrentSide = Side.SOURCE;
                SetItemsTextsIndex(TradeSourceMenuItems, ref _sourceItemsIndex);
            }
            else if (DestinationCharacter.Items.Count > 0)
            {
                CurrentSide = Side.DESTINATION;
                SetItemsTextsIndex(TradeDestinationMenuItems, ref _destinationItemsIndex);
            }
            else
            {
                Debug.LogError("Neither character has items to trade.");
            }

            transform.position = sourceCharacter.transform.position;

            Focus();
            transform.gameObject.SetActive(true);
        }

        private void SetItemsTextsIndex(List<TradeMenuItem> tradeMenuItems, ref int masterIndex, int index = 0)
        {
            // Remove indicator from previous text
            if (masterIndex < tradeMenuItems.Count)
            {
                TradeMenuItem previousItem = tradeMenuItems[masterIndex];
                previousItem.Text.text = previousItem.Text.text.Replace(Menu.INDICATOR, "");
            }

            // Update currently selected text
            masterIndex = index;
            TradeMenuItem currentItem = tradeMenuItems[masterIndex];
            currentItem.Text.text += Menu.INDICATOR;
        }

        public override void OnCancel()
        {
            foreach (TradeMenuItem tradeMenuItem in TradeSourceMenuItems)
            {
                Destroy(tradeMenuItem.Text.gameObject);
            }
            TradeSourceMenuItems.Clear();

            foreach (TradeMenuItem tradeMenuItem in TradeDestinationMenuItems)
            {
                Destroy(tradeMenuItem.Text.gameObject);
            }
            TradeDestinationMenuItems.Clear();

            transform.gameObject.SetActive(false);
            GameManager.Cursor.TradeDetailPanelOnClose();
        }

        public override void OnSubmit()
        {
            SourceCharacter.HasTraded = true;
            SourceCharacter.HasMoved = true;
            if (CurrentSide == Side.SOURCE)
            {
                TradeMenuItem tradeMenuItem = TradeSourceMenuItems[SourceItemsIndex];
                _ = SourceCharacter.Items.Remove(tradeMenuItem.Item);
                _ = TradeSourceMenuItems.Remove(tradeMenuItem);
                DestinationCharacter.Items.Add(tradeMenuItem.Item);
                TradeDestinationMenuItems.Add(new TradeMenuItem(TradeDestinationPanel, tradeMenuItem.Item));
                Destroy(tradeMenuItem.Text.gameObject);

                if (SourceCharacter.Items.Count == 0)
                {
                    CurrentSide = Side.DESTINATION;
                    SetItemsTextsIndex(TradeDestinationMenuItems, ref _destinationItemsIndex);
                }
                else
                {
                    Debug.LogFormat("Setting source index from {0} to {1}", SourceItemsIndex, Mathf.Min(SourceItemsIndex, TradeSourceMenuItems.Count - 1));
                    SetItemsTextsIndex(TradeSourceMenuItems, ref _sourceItemsIndex, Mathf.Min(SourceItemsIndex, TradeSourceMenuItems.Count - 1));
                }
            }
            else
            {
                TradeMenuItem tradeMenuItem = TradeDestinationMenuItems[DestinationItemsIndex];
                _ = DestinationCharacter.Items.Remove(tradeMenuItem.Item);
                _ = TradeDestinationMenuItems.Remove(tradeMenuItem);
                SourceCharacter.Items.Add(tradeMenuItem.Item);
                TradeSourceMenuItems.Add(new TradeMenuItem(TradeSourcePanel, tradeMenuItem.Item));
                Destroy(tradeMenuItem.Text.gameObject);

                if (DestinationCharacter.Items.Count == 0)
                {
                    CurrentSide = Side.SOURCE;
                    SetItemsTextsIndex(TradeSourceMenuItems, ref _sourceItemsIndex);
                }
                else
                {
                    SetItemsTextsIndex(TradeDestinationMenuItems, ref _destinationItemsIndex, Mathf.Min(DestinationItemsIndex, TradeDestinationMenuItems.Count - 1));
                }
            }
        }

        public override void OnInformation()
        {
            Debug.LogFormat("TradeDetailPanel.OnInformation is not implemented");
        }

        /// <summary>
        /// Information representing an item on the trade menu
        /// </summary>
        public class TradeMenuItem
        {
            public Item Item;
            public Text Text;

            public TradeMenuItem(Transform parent, Item item)
            {
                Item = item;
                Text = Instantiate(item.Text, parent);
            }
        }
    }
}