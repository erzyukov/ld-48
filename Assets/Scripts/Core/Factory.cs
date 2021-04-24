﻿using System;
using UnityEngine;
using Zenject;

public class Factory: ITickable
{

    private Settings settings;
    private Money money;
    private Dissatisfied dissatisfied;
    private Ecology ecology;
    private Type type;

    private int currentUpgrade = 0;

    private bool isBurning = false;

    private float currentAirPollutionPerSecond = 0;
    private float currentForestPollutionPerSecond = 0;
    private float currentWaterPollutionPerSecond = 0;
    private float currentStorageAmount = 0;

    public Factory(Type type, FactorySet[] settings, Money money, Dissatisfied dissatisfied, Ecology ecology)
    {
        this.settings = getSetingsByType(type, settings);
        this.money = money;
        this.dissatisfied = dissatisfied;
        this.ecology = ecology;
        this.type = type;

        currentAirPollutionPerSecond = this.settings.AirPollutionPerSecond;
        currentForestPollutionPerSecond = this.settings.ForestPollutionPerSecond;
        currentWaterPollutionPerSecond = this.settings.WaterPollutionPerSecond;
    }

    /// <summary>
    /// Проверяет есть ли свободное место на складе
    /// </summary>
    /// <returns>Есть ли место</returns>
    public bool IsStorageFull()
    {
        return currentStorageAmount >= settings.StorageCapacity;
    }

    /// <summary>
    /// Возвращает свободное место на складе
    /// </summary>
    /// <returns>Количество свободного места</returns>
    public float GetFreeStorageSpace()
    {
        return settings.StorageCapacity - currentStorageAmount;
    }

    /// <summary>
    /// Добавляет на склад заданное количество мусора
    /// Если добавить не получилось возвращает false
    /// </summary>
    /// <param name="amount">Количество мусора</param>
    /// <returns>Получилось ли добавить мусор</returns>
    public bool AddGarbageToStorage(float amount)
    {
        if (amount > GetFreeStorageSpace())
            return false;

        currentStorageAmount += amount;
        if (currentStorageAmount > 0 && !isBurning)
            StartBurn();

        return true;
    }

    /// <summary>
    /// Запустить завод
    /// </summary>
    public void StartBurn()
    {
        isBurning = true;
    }

    /// <summary>
    /// Остановить завод
    /// </summary>
    public void StopBurn()
    {
        isBurning = false;
    }

    /// <summary>
    /// Усовершенствовать завод
    /// </summary>
    /// <returns>Получилось ли обновить завод</returns>
    public bool DoUpgrade()
    {
        if (CanUpgrade())
        {
            var upgradeSettings = settings.Upgrades[currentUpgrade];

            if (money.SubtractMonet(upgradeSettings.UpgradeCost))
            {
                currentAirPollutionPerSecond = upgradeSettings.AirPollutionDecreaseAmount;
                currentForestPollutionPerSecond = upgradeSettings.ForesPollutionDecreaseAmount;
                currentWaterPollutionPerSecond = upgradeSettings.WaterPollutionDecreaseAmount;

                currentUpgrade++;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Возвращает стоимость следующего улучшения
    /// </summary>
    /// <returns>Стоимость следующего улучшения</returns>
    public int GetNextUpgradeCost()
    {
        if (CanUpgrade())
            return settings.Upgrades[currentUpgrade].UpgradeCost;
        return -1;
    }

    /// <summary>
    /// Проверяет можно ли улучшать текущий завод
    /// Если есть улучшения и хватает денег, то возвращает true
    /// </summary>
    /// <returns>Можно ли улучшить</returns>
    public bool CanUpgrade()
    {
        return 
            currentUpgrade < settings.Upgrades.Length && 
            money.moneyAmount >= settings.Upgrades[currentUpgrade].UpgradeCost;
    }

    public void Tick()
    {
        if (isBurning)
        {
            currentStorageAmount -= settings.BurnAmountPerTick * Time.deltaTime;
            if (currentStorageAmount < 0)
                StopBurn();
            IncreasePollution();
            dissatisfied.AddDissatisfied(settings.DissatisfiedAmountPerSecond * Time.deltaTime);
        }
    }

    private void IncreasePollution()
    {
        switch (type)
        {
            case Type.Water:
                ecology.AddParameter(Ecology.Type.Water, currentWaterPollutionPerSecond);
                ecology.AddParameter(Ecology.Type.Forest, currentForestPollutionPerSecond);
                ecology.AddParameter(Ecology.Type.Air, currentAirPollutionPerSecond);
                break;
            case Type.Forest:
                ecology.AddParameter(Ecology.Type.Forest, currentForestPollutionPerSecond);
                ecology.AddParameter(Ecology.Type.Air, currentAirPollutionPerSecond);
                break;
            case Type.Air:
                ecology.AddParameter(Ecology.Type.Air, currentAirPollutionPerSecond);
                break;
        }
    }

    private Settings getSetingsByType(Type type, FactorySet[] set)
    {
        for(var i = 0; i < set.Length; i++)
            if (set[i].type == type)
                return set[i].settings;

        throw new ArgumentOutOfRangeException();
    }

    #region Settings

    public enum Type
    {
        /// <summary>
        /// Загрязняет воздух
        /// </summary>
        Air,
        /// <summary>
        /// Загрязняет воздух и лес
        /// </summary>
        Forest,
        /// <summary>
        /// Загрязняет воздух и лес и вода
        /// </summary>
        Water,
    }

    [Serializable]
    public struct FactorySet
    {
        public Factory.Type type;
        public Factory.Settings settings;
    }

    [Serializable]
    public struct Settings
    {
        [Tooltip("Загрязнение воздуха")]
        public float AirPollutionPerSecond;

        [Tooltip("Загрязнение леса")]
        public float ForestPollutionPerSecond;

        [Tooltip("Загрязнение воды")]
        public float WaterPollutionPerSecond;

        [Tooltip("Вместимость хранилища завода")]
        public int StorageCapacity;

        [Tooltip("Недовольства в секунду")]
        public float DissatisfiedAmountPerSecond;

        [Tooltip("Сжигания в секунду")]
        public float BurnAmountPerTick;

        [Tooltip("Настройки улучшений")]
        public FactoryUpgradeSettings[] Upgrades;
    }

    #endregion
}