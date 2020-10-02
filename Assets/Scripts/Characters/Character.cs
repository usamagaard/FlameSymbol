using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TODO weapon Proficiencies
/// </summary>
public abstract class Character : ManagedMonoBehavior
{
    private Player player;
    public List<Item> Items = new List<Item>();

    public readonly List<Transform> MovableSpaces = new List<Transform>();
    public readonly List<Transform> AttackableSpaces = new List<Transform>();

    public string CharacterName;
    public int Strength;
    public int Magic;
    public int Skill;
    public int Resistance;
    public int Defense;
    public int Speed;
    public int MaxHp;
    public int CurrentHp;
    public int Moves;
    public int Level;
    public int Experience;
    public bool IsFlyer = false;

    public Player Player
    {
        get
        {
            return player;
        }

        set
        {
            player = value;
            player.Characters.Add(this);
        }
    }

    public List<Proficiency> Proficiencies = new List<Proficiency>();

    public void AddProficiency(Proficiency proficiency)
    {
        foreach (Proficiency p in Proficiencies)
        {
            if (p.type.Equals(proficiency.type))
            {
                Proficiencies.Remove(p);
                break;
            }
        }

        Proficiencies.Add(proficiency);
    }

    /// <summary>
    /// Calculate ranges based on the character's items.
    /// </summary>
    /// <returns></returns>
    public HashSet<int> GetWeaponRanges()
    {
        HashSet<int> ranges = new HashSet<int>();

        foreach (Item item in GetUsableWeapons())
        {
            if (item is Weapon)
            {
                Weapon weapon = (Weapon)item;

                if (IsProficient(weapon))
                {
                    ranges.UnionWith(weapon.Ranges);
                }
            }
        }

        return ranges;
    }

    public HashSet<Vector2> CalculateMovablePositions()
    {
        return CalculateMovablePositions(transform.position.x, transform.position.y, Moves);
    }

    private HashSet<Vector2> CalculateMovablePositions(float x, float y, int remainingMoves)
    {
        HashSet<Vector2> movableSpaces = new HashSet<Vector2>();
        Character character = GameManager.CurrentLevel.GetCharacter(x, y);
        Terrain.Terrain terrain = GameManager.CurrentLevel.GetTerrain(x, y);
        if (remainingMoves <= 0 || GameManager.CurrentLevel.IsOutOfBounds(x, y) || !terrain.IsPassable(this, x, y) || (character != null && !character.Player.Equals(Player)))
        {
            return movableSpaces;
        }

        Vector2 position = new Vector2(x, y);
        remainingMoves -= CalculateMovementCost(position);

        _ = movableSpaces.Add(position);

        movableSpaces.UnionWith(CalculateMovablePositions(x - 1, y, remainingMoves));
        movableSpaces.UnionWith(CalculateMovablePositions(x + 1, y, remainingMoves));
        movableSpaces.UnionWith(CalculateMovablePositions(x, y - 1, remainingMoves));
        movableSpaces.UnionWith(CalculateMovablePositions(x, y + 1, remainingMoves));
        return movableSpaces;
    }

    public List<Transform> CreateMovableTransforms()
    {
        return CreateMovableTransforms(CalculateMovablePositions());
    }

    /// <summary>
    /// Creates the movable transforms from the collection of movable positions
    /// </summary>
    /// <param name="movablePositions"></param>
    /// <returns></returns>
    public List<Transform> CreateMovableTransforms(ICollection<Vector2> movablePositions)
    {
        foreach (Vector2 movablePosition in movablePositions)
        {
            MovableSpaces.Add(Instantiate(GameManager.MovableSpacePrefab, new Vector2(movablePosition.x, movablePosition.y), Quaternion.identity, GameManager.transform));
        }

        return MovableSpaces;
    }

    /// <summary>
    /// Calculates the attackable positions for all of the character's movable positions.
    /// </summary>
    /// <returns></returns>
    public HashSet<Vector2> CalculateAttackablePositions()
    {
        HashSet<Vector2> attackablePositions = new HashSet<Vector2>();
        HashSet<Vector2> movablePositions = CalculateMovablePositions();

        HashSet<int> ranges = GetWeaponRanges();

        foreach (Vector2 movablePosition in movablePositions)
        {
            attackablePositions.UnionWith(CalculateAttackablePositions(movablePosition.x, movablePosition.y, ranges));
        }

        return attackablePositions;
    }

    /// <summary>
    /// Calculates the attackable positions for the given position and the ranges (e.g. of weapons)
    /// </summary>
    /// <param name="x">The x coordinate of the position</param>
    /// <param name="y">The y coordinate of the position</param>
    /// <param name="ranges">The list of ranges</param>
    /// <returns>The list of attackable positions</returns>
    public HashSet<Vector2> CalculateAttackablePositions(float x, float y, HashSet<int> ranges)
    {
        HashSet<Vector2> attackablePositions = new HashSet<Vector2>();
        foreach (int range in ranges)
        {
            attackablePositions.UnionWith(CalculateAttackablePositions(x, y, range));
        }
        return attackablePositions;
    }

    public void AddExperience(int experience)
    {
        if (experience > 100)
        {
            Debug.LogErrorFormat("Experience cannot be greater than 100: {0}", experience);
            return;
        }

        Experience += experience;
        if (Experience >= 100)
        {
            LevelUp();
            Experience %= experience;
        }
    }

    public void LevelUp()
    {
        Debug.Log("Level up!");
        Level += 1;
        Strength += GameManager.Random.Next(0, 2);
        Magic += GameManager.Random.Next(0, 2);
        Skill += GameManager.Random.Next(0, 2);
        Resistance += GameManager.Random.Next(0, 2);
        Defense += GameManager.Random.Next(0, 2);
        MaxHp += GameManager.Random.Next(0, 2);
    }

    public HashSet<Vector2> CalculateAttackablePositions(float x, float y, int range)
    {
        HashSet<Vector2> attackablePositions = new HashSet<Vector2>();
        if (range < 0 || GameManager.CurrentLevel.IsOutOfBounds(x, y))
        {
            return attackablePositions;
        }

        if (range == 0)
        {
            attackablePositions.Add(new Vector2(x, y));
            return attackablePositions;
        }

        attackablePositions.UnionWith(CalculateAttackablePositions(x + 1, y, range - 1));
        attackablePositions.UnionWith(CalculateAttackablePositions(x - 1, y, range - 1));
        attackablePositions.UnionWith(CalculateAttackablePositions(x, y + 1, range - 1));
        attackablePositions.UnionWith(CalculateAttackablePositions(x, y - 1, range - 1));

        return attackablePositions;
    }

    /// <summary>
    /// Creates the movable and attackable transforms.
    ///
    /// Will not delete previous movable and attackable transforms.
    /// </summary>
    /// <returns>The list of all attackable transforms</returns>
    public List<Transform> CreateAttackableTransforms()
    {
        HashSet<Vector2> movablePositions = CalculateMovablePositions();
        CreateMovableTransforms(movablePositions);

        return CreateAttackableTransforms(CalculateAttackablePositions(), movablePositions);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="attackablePositions"></param>
    /// <param name="movablePositions"></param>
    /// <returns></returns>
    public List<Transform> CreateAttackableTransforms(ICollection<Vector2> attackablePositions, ICollection<Vector2> movablePositions = null)
    {
        movablePositions = movablePositions ?? new HashSet<Vector2>();
        foreach (Vector2 attackablePosition in attackablePositions)
        {
            if (movablePositions.Contains(attackablePosition))
            {
                continue;
            }

            AttackableSpaces.Add(Instantiate(GameManager.AttackableSpacePrefab, new Vector2(attackablePosition.x, attackablePosition.y), Quaternion.identity, GameManager.transform));
        }

        return AttackableSpaces;
    }

    /// <summary>
    /// Move the Character to the specified position
    /// </summary>
    /// <param name="position"></param>
    public void Move(Vector2 position)
    {
        if (transform.position.x == position.x && transform.position.y == position.y)
        {
            return;
        }

        if (GameManager.CurrentLevel.GetCharacter(position) != null)
        {
            Debug.LogErrorFormat("Position is already taken: {0}", position);
        }
        Vector2 oldPosition = transform.position;
        GameManager.CurrentLevel.SetCharacter(this, position);

        if (position.x != oldPosition.x || position.y != oldPosition.y)
        {
            GameManager.CurrentLevel.SetCharacter(null, oldPosition);
        }
    }

    public int CalculateMovementCost(Vector2 position)
    {
        int cost;
        Debug.LogFormat("Calculating movement cost for: {0}", position);
        if (position.Equals(transform.position))
        {
            Debug.LogFormat("The same position");
            cost = 0;
        }
        else
        {
            cost = CalculateMovementCost(position.x, position.y);
        }
        Debug.LogFormat("Movement cost: {0}", cost);
        return cost;
    }

    /// <summary>
    /// Calculates the cost for this character to move to the given position.
    /// TODO add terrain considerations
    /// </summary>
    /// <param name="position">The position for which to determine the cost</param>
    /// <returns>The cost for this character to move to the given position</returns>
    protected virtual int CalculateMovementCost(float x, float y)
    {
        if (GameManager.CurrentLevel.IsOutOfBounds(x, y))
        {
            return int.MaxValue;
        }

        Terrain.Terrain terrain = GameManager.CurrentLevel.GetTerrain(x, y);
        Debug.LogFormat("Terrain movement cost: {0}", terrain.DisplayName);
        return terrain.MovementCost;
    }

    /// <summary>
    /// Destory attackable transforms
    /// </summary>
    public void DestroyAttackableTransforms()
    {
        Debug.LogFormat("Destroying {0} attackable spaces", AttackableSpaces.Count);
        foreach (Transform attackableSpace in AttackableSpaces)
        {
            Destroy(attackableSpace.gameObject);
        }
        AttackableSpaces.Clear();
    }

    /// <summary>
    /// Destroy movable transforms
    /// </summary>
    public void DestroyMovableTransforms()
    {
        Debug.LogFormat("Destroying {0} movable spaces", MovableSpaces.Count);
        foreach (Transform movableSpace in MovableSpaces)
        {
            Destroy(movableSpace.gameObject);
        }

        MovableSpaces.Clear();
    }

    /// <summary>
    /// Destroys both movable and attackable transforms
    /// </summary>
    public void DestroyMovableAndAttackableTransforms()
    {
        DestroyMovableTransforms();
        DestroyAttackableTransforms();
    }

    /// <summary>
    /// Calculate tradable spaces for this Character
    /// </summary>
    /// <returns></returns>
    public List<Transform> CalculateTradableSpaces()
    {
        List<Transform> tradableSpaces = new List<Transform>();
        float y = transform.position.x;
        float x = transform.position.y;

        CalculateTradableSpace(x - 1, y, tradableSpaces);
        CalculateTradableSpace(x + 1, y, tradableSpaces);
        CalculateTradableSpace(x, y - 1, tradableSpaces);
        CalculateTradableSpace(x, y + 1, tradableSpaces);

        return tradableSpaces;
    }

    /// <summary>
    /// Calculate the tradable spaces and create the transforms
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tradableSpaces"></param>
    private void CalculateTradableSpace(float x, float y, List<Transform> tradableSpaces)
    {
        Character character = GameManager.CurrentLevel.GetCharacter(x, y);
        if (character != null && character.Player.Equals(Player))
        {
            tradableSpaces.Add(Instantiate(GameManager.AttackableSpacePrefab, new Vector2(x, y), Quaternion.identity, GameManager.transform));
        }
    }

    /// <summary>
    /// Calculate the attackable spaces that contain characters
    /// </summary>
    /// <returns></returns>
    public List<Transform> GetAttackableSpacesWithCharacters()
    {
        DestroyAttackableTransforms();
        HashSet<int> ranges = GetWeaponRanges();

        foreach (int range in ranges)
        {
            CreateAttackableTransforms(CalculateAttackablePositions(transform.position.x, transform.position.y, range));
        }

        AttackableSpaces.RemoveAll(attackableSpace =>
        {
            Character defendingCharacter = GameManager.CurrentLevel.GetCharacter(attackableSpace.position);

            if (defendingCharacter == null || defendingCharacter.Player.Equals(Player))
            {
                Destroy(attackableSpace.gameObject);
                return true;
            }

            return false;
        });

        return AttackableSpaces;
    }

    /// <summary>
    /// Equip the item from the Character's inventory.
    /// </summary>
    /// <param name="item"></param>
    public void Equip(Item item)
    {
        if (Items.Count > 1)
        {
            if (!Items.Remove(item))
            {
                Debug.LogErrorFormat("{0} does not exist in inventory.", item.Text.text);
            }
            Items.Insert(0, item);
        }
    }

    /// <summary>
    /// Called when a Character dies
    /// </summary>
    public virtual void Die()
    {
        Debug.LogFormat("Die: {0}", this);
        Destroy(gameObject);
        Debug.Assert(Player.Characters.Remove(this));
        Debug.LogFormat("Remaining characters: {0}", Player.Characters.Count);
    }

    /// <summary>
    /// Attack the specified Character and apply damage, effects, etc
    ///
    /// TODO make criticals a thing
    /// TODO make speed a thing
    /// </summary>
    /// <param name="defenseCharacter">The Character to attack</param>
    public void Attack(Character defenseCharacter)
    {
        AttackInfo attackInfo = CalculateAttackInfo(defenseCharacter);

        // TODO use actual hit percentage
        int attackHitPercentage = GameManager.Random.Next(100);

        int attackExperience = 1;
        int defenseExperience = 0;

        if (attackHitPercentage <= attackInfo.AttackHitPercentage)
        {
            attackInfo.AttackWeapon.Use();
            if (attackInfo.AttackDamage != 0)
            {
                attackExperience += 9;
                defenseCharacter.CurrentHp = Mathf.Max(0, defenseCharacter.CurrentHp - attackInfo.AttackDamage);
            }
        }

        if (defenseCharacter.CurrentHp == 0)
        {
            attackExperience += 10;
            defenseCharacter.Die();
        }
        else
        {
            defenseExperience += 1;

            int defenseHitPercentage = GameManager.Random.Next(100);
            if (defenseHitPercentage <= attackInfo.DefenseHitPercentage)
            {
                attackInfo.DefenseWeapon.Use();
                if (attackInfo.DefenseDamage != 0)
                {
                    defenseExperience += 9;
                    CurrentHp = Mathf.Max(0, CurrentHp - attackInfo.DefenseDamage);
                }
            }

            if (CurrentHp == 0)
            {
                defenseExperience += 10;
                defenseCharacter.AddExperience(defenseExperience);
                Die();
                return;
            }
        }

        AddExperience(attackExperience);
    }

    /// <summary>
    /// Calculate the attack information for the attack on the specified defense Character
    /// </summary>
    /// <param name="defenseCharacter"></param>
    /// <returns></returns>
    public AttackInfo CalculateAttackInfo(Character defenseCharacter)
    {
        Weapon attackWeapon = GetUsableWeapon();
        Debug.LogFormat("Attack weapon: {0}", attackWeapon);

        Weapon defenseWeapon = null;
        List<Weapon> usableWeapons = defenseCharacter.GetUsableWeapons();
        bool defenseCanAttack;
        if (usableWeapons.Count > 0)
        {
            defenseWeapon = usableWeapons[0];
            Debug.LogFormat("Defense weapon: {0}", defenseWeapon);
            defenseCanAttack = defenseWeapon.IsInRange(defenseCharacter.transform.position, transform.position);
        }
        else
        {
            defenseCanAttack = false;
        }

        int attackHitPercentage = CalculateHitPercentage(this, attackWeapon, defenseCharacter);
        int attackDamage = CalculateDamage(this, attackWeapon, defenseCharacter);
        int attackCriticalPercentage = CalculateCriticalPercentage(this, attackWeapon, defenseCharacter);

        int defenseHitPercentage = 0;
        int defenseDamage = 0;
        int defenseCriticalPercentage = 0;

        if (defenseCanAttack)
        //if (defenseWeapon != null)
        {
            defenseHitPercentage = CalculateHitPercentage(defenseCharacter, defenseWeapon, this);
            defenseDamage = CalculateDamage(defenseCharacter, defenseWeapon, this);
            defenseCriticalPercentage = CalculateCriticalPercentage(defenseCharacter, defenseWeapon, this);
        }

        return new AttackInfo(attackWeapon, attackHitPercentage, attackDamage, attackCriticalPercentage, defenseWeapon, defenseHitPercentage, defenseDamage, defenseCriticalPercentage, defenseCanAttack);
    }

    /// <summary>
    /// Calculate the hit percentage
    ///
    /// TODO add terrain considerations
    /// </summary>
    /// <param name="attackCharacter"></param>
    /// <param name="attackWeapon"></param>
    /// <param name="defenseCharacter"></param>
    /// <returns></returns>
    private int CalculateHitPercentage(Character attackCharacter, Weapon attackWeapon, Character defenseCharacter)
    {
        return Mathf.Clamp(attackCharacter.Skill + attackWeapon.HitPercentage - defenseCharacter.Speed, 0, 100);
    }

    /// <summary>
    /// Calculate the damage done by the attack Character with the specified weapon and the defense Character
    ///
    /// TODO Add terrain considerations
    /// </summary>
    /// <param name="attackCharacter"></param>
    /// <param name="attackWeapon"></param>
    /// <param name="defenseCharacter"></param>
    /// <returns></returns>
    private int CalculateDamage(Character attackCharacter, Weapon attackWeapon, Character defenseCharacter)
    {
        int damage = attackWeapon.CalculateDamage(defenseCharacter);
        if (attackWeapon is StrengthWeapon)
        {
            damage += attackCharacter.Strength - defenseCharacter.Defense;
        }
        else if (attackWeapon is MagicWeapon)
        {
            damage += attackCharacter.Magic - defenseCharacter.Resistance;
        }
        else
        {
            Debug.LogError("Unknown weapon type");
        }

        return Mathf.Max(damage, 0);
    }

    private int CalculateCriticalPercentage(Character attackCharacter, Weapon attackWeapon, Character defenseCharacter)
    {
        return Mathf.Clamp((attackCharacter.Skill / 4) + attackWeapon.CriticalPercentage, 0, 100);
    }

    /// <summary>
    /// Get all weapons that are usable by the Character
    /// </summary>
    /// <returns></returns>
    public List<Weapon> GetUsableWeapons()
    {
        List<Weapon> weapons = new List<Weapon>();
        foreach (Item item in Items)
        {
            if (item is Weapon weapon && IsProficient(weapon))
            {
                weapons.Add(weapon);
            }
        }

        return weapons;
    }

    /// <summary>
    /// Whether or not this character is proficient (is able to wield) the weapon in question.
    /// </summary>
    /// <param name="weapon"></param>
    /// <returns></returns>
    public bool IsProficient(Weapon weapon)
    {
        Debug.LogFormat("Is {0} proficient with {1}", CharacterName, weapon);
        foreach (Proficiency proficiency in Proficiencies)
        {
            Debug.LogFormat("Proficiency: {0}", proficiency);
            if (weapon.GetType().IsSubclassOf(proficiency.type))
            {
                Debug.LogFormat("{0} is sub type of {1}", weapon.GetType(), proficiency.type);
                if (proficiency.rank >= weapon.RequiredProficiencyRank)
                {
                    Debug.Log("Yes!");
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the first usable weapon for the Character
    /// </summary>
    /// <returns></returns>
    public Weapon GetUsableWeapon()
    {
        List<Weapon> weapons = GetUsableWeapons();
        if (weapons.Count > 0)
        {
            return weapons[0];
        }

        return null;
    }

    /// <summary>
    /// Represents an attack/defense scenario for two Characters
    /// </summary>
    public class AttackInfo
    {
        public Weapon AttackWeapon;
        public int AttackHitPercentage;
        public int AttackDamage;
        public int AttackCriticalPercentage;

        public Weapon DefenseWeapon;
        public int DefenseHitPercentage;
        public int DefenseDamage;
        public int DefenseCriticalPercentage;
        public bool DefenseCanAttack;

        public AttackInfo(Weapon attackWeapon, int attackHitPercentage, int attackDamage, int attackCriticalPercentage, Weapon defenseWeapon, int defenseHitPercentage, int defenseDamage, int defenseCriticalPercentage, bool defenseCanAttack)
        {
            AttackWeapon = attackWeapon;
            AttackHitPercentage = attackHitPercentage;
            AttackDamage = attackDamage;
            AttackCriticalPercentage = attackCriticalPercentage;

            DefenseWeapon = defenseWeapon;
            DefenseHitPercentage = defenseHitPercentage;
            DefenseDamage = defenseDamage;
            DefenseCriticalPercentage = defenseCriticalPercentage;
            DefenseCanAttack = defenseCanAttack;
        }
    }
}
