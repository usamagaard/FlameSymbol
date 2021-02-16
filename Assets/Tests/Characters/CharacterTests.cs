﻿using System.Collections;
using System.Collections.Generic;
using Characters;
using Items;
using Items.Weapons;
using Items.Weapons.Assistable.HealingStaffs;
using Items.Weapons.Attackable;
using Items.Weapons.Attackable.Magic.FireMagic;
using Items.Weapons.Attackable.Strength.Lance;
using Items.Weapons.Attackable.Strength.Sword;
using Logic;
using NUnit.Framework;
using UI;
using UnityEngine;
using UnityEngine.TestTools;
using Cursor = UI.Cursor;

namespace Tests.Characters
{
    public class CharacterTests : GameManagerTest
    {

        /// <summary>
        /// Checking that everything occurs correctly when attacking.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator AttackTest1()
        {
            yield return MoveCursor(2, 2);

            // Select Character
            yield return Submit(GameManager.Cursor);

            // Select Move
            yield return Submit(GameManager.Cursor);

            // Attack is in the list of actions
            Assert.True(GameManager.CharacterActionMenu.MenuItems.Exists((Menu.MenuItem<Item> obj) => { return obj.DisplayText.text.Contains("Attack"); })); ; ;

            // Select Attack
            GameManager.CharacterActionMenu.OnSubmit();
            yield return null;

            // Character has one option for weapons
            Assert.AreEqual(1, GameManager.ItemSelectionMenu.MenuItems.Count);

            // Select Weapon
            GameManager.ItemSelectionMenu.OnSubmit();
            yield return null;

            // Character has four spaces with Characters
            Assert.AreEqual(4, GameManager.Cursor.AttackableSpacesWithCharacters.Count);

            // Select Character to attack
            yield return Submit(GameManager.Cursor);

            Character character = GameManager.CurrentLevel.GetCharacter(1, 2);
            Assert.AreNotEqual(character.MaxHp, character.CurrentHp);
        }

        /// <summary>
        /// Testing to make sure everything is cleaned up after character death
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator DieTest1()
        {
            ICollection<Character> beforeCharacters = GameManager.CurrentLevel.GetCharacters();

            yield return MoveCursor(2, 2);

            yield return Submit(GameManager.Cursor);

            yield return Submit(GameManager.Cursor);

            GameManager.CharacterActionMenu.OnSubmit();
            yield return null;

            GameManager.ItemSelectionMenu.OnSubmit();
            yield return null;

            yield return Submit(GameManager.Cursor);

            yield return MoveCursor(2, 2);

            yield return Submit(GameManager.Cursor);
            yield return Submit(GameManager.Cursor);

            GameManager.CharacterActionMenu.OnSubmit();
            yield return null;

            GameManager.ItemSelectionMenu.OnSubmit();
            yield return null;

            yield return Submit(GameManager.Cursor);

            Assert.IsNull(GameManager.CurrentLevel.GetCharacter(1, 2));

            foreach (Character character in GameManager.CurrentLevel.GetCharacters())
            {
                Assert.IsEmpty(character.MovableTransforms);
                Assert.IsEmpty(character.MovableTransforms);
            }

            ICollection<Character> afterCharacters = GameManager.CurrentLevel.GetCharacters();

            Assert.AreEqual(afterCharacters.Count, beforeCharacters.Count - 1);
        }

        /// <summary>
        /// When the cursor is over a player-owned character
        /// and the character has a staff
        /// and the cursor is in stage choosing move
        /// Then the character should have staffable transforms
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator AssistableTransformsTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(2, 2);

            character.Items.Clear();
            character.Items.Add(Heal.Create());
            character.AddProficiency(new Proficiency(typeof(HealingStaff), rank: Proficiency.Rank.E));

            yield return MoveCursor(2, 2);

            Assert.IsNotEmpty(character.AssistableTransforms);
            Assert.AreEqual(5, character.AssistableTransforms.Count);

            AssertHelper.Contains(new Vector3(0, 0), character.AssistableTransforms);
            AssertHelper.Contains(new Vector3(0, 1), character.AssistableTransforms);
            AssertHelper.Contains(new Vector3(1, 2), character.AssistableTransforms);
            AssertHelper.Contains(new Vector3(2, 3), character.AssistableTransforms);
            AssertHelper.Contains(new Vector3(3, 2), character.AssistableTransforms);
        }

        /// <summary>
        /// When the character has a staff
        /// and there is a staffable character within range
        /// then the 'Assist' option should show up in the character action menu
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator HealingTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(2, 2);

            character.Items.Clear();
            character.Items.Add(Heal.Create());
            character.AddProficiency(new Proficiency(typeof(HealingStaff), rank: Proficiency.Rank.E));

            yield return MoveCursor(2, 2);

            yield return Submit(GameManager.Cursor);

            yield return Submit(GameManager.Cursor);

            Assert.IsNotEmpty(GameManager.CharacterActionMenu.MenuItems);

            Assert.True(GameManager.CharacterActionMenu.MenuItems.Exists((Menu.MenuItem<Item> obj) => { return obj.DisplayText.text.Contains("Assist"); })); ; ;
        }

        /// <summary>
        /// Checking to make sure heal works
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator HealingTest2()
        {
            Character hurtCharacter = GameManager.CurrentLevel.GetCharacter(2, 1);
            hurtCharacter.CurrentHp -= 2;

            Character healingCharacter = GameManager.CurrentLevel.GetCharacter(2, 2);
            healingCharacter.Items.Clear();
            healingCharacter.Items.Add(Heal.Create());
            healingCharacter.AddProficiency(new Proficiency(typeof(HealingStaff), rank: Proficiency.Rank.E));

            yield return MoveCursor(2, 2);

            // select character
            yield return Submit(GameManager.Cursor);

            // select move
            yield return Submit(GameManager.Cursor);

            // select Assist
            GameManager.CharacterActionMenu.OnSubmit();

            // select staff
            GameManager.ItemSelectionMenu.OnSubmit();

            //select other character
            GameManager.Cursor.OnSubmit();

            Assert.False(GameManager.ItemSelectionMenu.IsInFocus());
            Assert.IsEmpty(healingCharacter.AssistableTransforms);
            Assert.IsEmpty(healingCharacter.MovableTransforms);
            Assert.IsEmpty(healingCharacter.AttackableTransforms);
            Assert.AreEqual(hurtCharacter.CurrentHp, hurtCharacter.MaxHp);
        }

        /// <summary>
        /// Making sure both attackable and assistable positions show up.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator HealingTest3()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(2, 2);
            Heal heal = Heal.Create();
            _ = heal.Ranges.Add(3);
            character.Items.Add(heal);

            yield return MoveCursor(2, 2);

            Assert.AreEqual(8, character.AttackableTransforms.Count);
            Assert.AreEqual(1, character.AssistableTransforms.Count);
        }

        /// <summary>
        /// Initial test to make sure everything shows up correctly
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TradingTest1()
        {
            Character sourceCharacter = GameManager.CurrentLevel.GetCharacter(2, 2);
            Character targetCharacter = GameManager.CurrentLevel.GetCharacter(2, 1);
            sourceCharacter.Items.Clear();

            // Move cursor
            yield return MoveCursor(2, 2);

            // select character
            yield return Submit(GameManager.Cursor);

            // select move
            yield return Submit(GameManager.Cursor);
            Assert.True(GameManager.CharacterActionMenu.IsInFocus());

            // select trade
            GameManager.CharacterActionMenu.OnSubmit();
            Assert.AreEqual(Cursor.State.ChoosingTradeTarget, GameManager.Cursor.CurrentState);

            // select trading character
            yield return Submit(GameManager.Cursor);
            Assert.True(GameManager.TradeDetailPanel.IsInFocus());

            Assert.AreEqual(sourceCharacter.CharacterName, GameManager.TradeDetailPanel.SourceText.text);
            Assert.AreEqual(targetCharacter.CharacterName, GameManager.TradeDetailPanel.DestinationText.text);

            sourceCharacter.Items.ForEach(
                sourceCharacterItem =>
                {
                    Debug.LogFormat("Item: {0}", sourceCharacterItem.Text.text);
                    Assert.True(
                        GameManager.TradeDetailPanel.TradeSourceMenuItems.Exists(sourceTradeItem => sourceCharacterItem.name.Equals(sourceTradeItem.Text.text)));
                }
            );
        }

        /// <summary>
        /// Test trading items
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TradingTest2()
        {
            Character sourceCharacter = GameManager.CurrentLevel.GetCharacter(2, 2);
            sourceCharacter.Items.Clear();
            sourceCharacter.Items.Add(IronSword.Create());
            sourceCharacter.Items.Add(Fire.Create());

            Character targetCharacter = GameManager.CurrentLevel.GetCharacter(2, 1);
            targetCharacter.Items.Clear();
            targetCharacter.Items.Add(Fire.Create());

            // Move cursor
            yield return MoveCursor(2, 2);

            // select character
            yield return Submit(GameManager.Cursor);

            // select move
            yield return Submit(GameManager.Cursor);

            // select trade
            yield return DownArrow(GameManager.CharacterActionMenu);
            yield return Submit(GameManager.CharacterActionMenu);

            // select trading character
            yield return Submit(GameManager.Cursor);

            yield return Submit(GameManager.TradeDetailPanel);
            Assert.AreEqual(1, sourceCharacter.Items.Count);
            Assert.AreEqual(2, targetCharacter.Items.Count);

            yield return DownArrow(GameManager.TradeDetailPanel);
            Assert.AreEqual(TradeDetailPanel.Side.SOURCE, GameManager.TradeDetailPanel.CurrentSide);

            // Press right
            yield return RightArrow(GameManager.TradeDetailPanel);
            Assert.AreEqual(TradeDetailPanel.Side.DESTINATION, GameManager.TradeDetailPanel.CurrentSide);

            yield return DownArrow(GameManager.TradeDetailPanel);
            Assert.AreEqual(TradeDetailPanel.Side.DESTINATION, GameManager.TradeDetailPanel.CurrentSide);

            yield return DownArrow(GameManager.TradeDetailPanel);
            Assert.AreEqual(TradeDetailPanel.Side.DESTINATION, GameManager.TradeDetailPanel.CurrentSide);

            yield return LeftArrow(GameManager.TradeDetailPanel);
            Assert.AreEqual(TradeDetailPanel.Side.SOURCE, GameManager.TradeDetailPanel.CurrentSide);

            yield return LeftArrow(GameManager.TradeDetailPanel);
            Assert.AreEqual(TradeDetailPanel.Side.SOURCE, GameManager.TradeDetailPanel.CurrentSide);

            yield return RightArrow(GameManager.TradeDetailPanel);
            Assert.AreEqual(TradeDetailPanel.Side.DESTINATION, GameManager.TradeDetailPanel.CurrentSide);

            yield return Submit(GameManager.TradeDetailPanel);
            Assert.AreEqual(2, sourceCharacter.Items.Count);
            Assert.AreEqual(1, targetCharacter.Items.Count);
        }

        /// <summary>
        /// Test canceling the trade
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TradingTest3()
        {
            Character sourceCharacter = GameManager.CurrentLevel.GetCharacter(2, 2);
            sourceCharacter.Items.Clear();
            sourceCharacter.Items.Add(IronSword.Create());
            sourceCharacter.Items.Add(Fire.Create());

            Character targetCharacter = GameManager.CurrentLevel.GetCharacter(2, 1);
            targetCharacter.Items.Clear();
            targetCharacter.Items.Add(Fire.Create());

            // Move cursor
            yield return MoveCursor(2, 2);

            // select character
            yield return Submit(GameManager.Cursor);

            // select move
            yield return Submit(GameManager.Cursor);

            // select trade
            yield return DownArrow(GameManager.CharacterActionMenu);
            yield return Submit(GameManager.CharacterActionMenu);

            // select trading character
            yield return Submit(GameManager.Cursor);

            yield return Cancel(GameManager.TradeDetailPanel);
            Assert.False(GameManager.TradeDetailPanel.IsInFocus());
            Assert.True(GameManager.Cursor.IsInFocus());
            Assert.AreEqual(Cursor.State.ChoosingTradeTarget, GameManager.Cursor.CurrentState);
        }

        /// <summary>
        /// Test calling trade when no items are tradable
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TradingTest4()
        {
            Character sourceCharacter = GameManager.CurrentLevel.GetCharacter(2, 2);
            sourceCharacter.Items.Clear();

            Character targetCharacter = GameManager.CurrentLevel.GetCharacter(2, 1);
            targetCharacter.Items.Clear();

            GameManager.TradeDetailPanel.Show(sourceCharacter, targetCharacter);

            LogAssert.Expect(LogType.Error, "Neither character has items to trade.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator CalculateAssistablePositionsTest1()
        {
            HashSet<int> ranges = new HashSet<int>();
            ranges.Add(1);
            ranges.Add(2);

            Character character = GameManager.CurrentLevel.GetCharacter(0, 0);

            HashSet<Vector2> assistablePositions = character.CalculateAssistablePositions(0, 0, ranges);
            Assert.AreEqual(6, assistablePositions.Count);

            yield return null;
        }

        [UnityTest]
        public IEnumerator AddExperienceTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(0, 0);
            int currentLevel = character.Level;
            character.AddExperience(100);
            Assert.AreEqual(currentLevel + 1, character.Level);

            yield return null;
        }

        [UnityTest]
        public IEnumerator AddExperienceTest2()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(0, 0);
            character.AddExperience(101);

            LogAssert.Expect(LogType.Error, "Experience cannot be greater than 100: 101");

            yield return null;
        }

        /// <summary>
        /// Test to make sure characters don't move on top of each other
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator MoveTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(0, 0);

            character.Move(new Vector2(0, 1));
            LogAssert.Expect(LogType.Error, "Position is already taken: (0.0, 1.0)");
            yield return null;
        }

        /// <summary>
        /// Test to make sure character move actually works
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator MoveTest2()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(0, 0);

            character.Move(new Vector2(1, 0));

            Assert.AreEqual(character, GameManager.CurrentLevel.GetCharacter(1, 0));
            yield return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator CalculateMovementCostTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(0, 0);

            Assert.AreEqual(int.MaxValue, character.CalculateMovementCost(new Vector2(-1, 0)));
            yield return null;
        }

        /// <summary>
        /// Equiping an item that the character does not have in their inventory
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator EquipTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(0, 0);
            Item item = IronSword.Create();

            character.Equip(item);
            LogAssert.Expect(LogType.Error, "Iron Sword does not exist in inventory.");

            yield return null;
        }

        /// <summary>
        /// Enemy attacks back
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator AttackTest2()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(2, 2);

            GameManager.CurrentLevel.SetCharacter(character, 1, 0);

            character.Attack(GameManager.CurrentLevel.GetCharacter(0, 0));

            yield return null;
        }

        /// <summary>
        /// Defending character kills attacking character
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator AttackTest3()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(2, 2);
            character.CurrentHp = 1;

            GameManager.CurrentLevel.SetCharacter(character, 1, 0);

            character.Attack(GameManager.CurrentLevel.GetCharacter(0, 0));

            yield return null;
        }

        /// <summary>
        /// Calculate damage with unknown weapon type
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator CalculateDamageTest1()
        {
            Character attackCharacter = GameManager.CurrentLevel.GetCharacter(2, 2);
            Character defenseCharacter = GameManager.CurrentLevel.GetCharacter(2, 1);

            Weapon weapon = ScriptableObject.CreateInstance<Items.Weapons.WeaponTests.FakeWeapon>();

            _ = attackCharacter.CalculateDamage(weapon, defenseCharacter);
            LogAssert.Expect(LogType.Error, "Unknown weapon type: FakeWeapon");
            yield return null;
        }

        /// <summary>
        /// Testing when Character does not have a usable item
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator GetUsableItemTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(2, 2);
            character.Proficiencies.Clear();

            Assert.IsNull(character.GetUsableItem<Attackable>());
            yield return null;
        }

        /// <summary>
        /// Checking to make sure when character has an item's proficiency but is not proficient enough
        /// then IsProficient returns false.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator IsProficientTest1()
        {
            Character character = GameManager.CurrentLevel.GetCharacter(2, 2);
            character.Proficiencies.Clear();
            character.AddProficiency(new Proficiency(typeof(Sword), Proficiency.Rank.A));

            character.Items.Clear();
            IronLance ironLance = IronLance.Create();
            ironLance.RequiredProficiencyRank = Proficiency.Rank.S;
            character.Items.Add(ironLance);

            Assert.False(character.IsProficient(ironLance));
            yield return null;
        }
    }
}
