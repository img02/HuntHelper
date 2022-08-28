using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HuntHelper.Managers.Hunts.Models;

namespace HuntHelper.Managers;

internal struct AetheryteData
{
    internal uint AetheryteID;
    internal byte Subindex; //seems to always be 0 for non-housing
    internal uint TerritoryID;
    //in-game coords
    internal Vector2 Position;
}

public unsafe class TeleportManager
{
    private List<AetheryteData> Aetherytes = new List<AetheryteData>()
    {
        #region ARR aetherytes
        //ARR
        new AetheryteData()
            {AetheryteID = 8, Subindex = 0, TerritoryID = 129, Position = new Vector2(0)}, //Limsa Lominsa Lower Decks
        //middle la noscea
        new AetheryteData() {AetheryteID = 52, Subindex = 0, TerritoryID = 134, Position = new Vector2(26f, 16.3f)}, //Summerford Farms
        //lower la noscea
        new AetheryteData() {AetheryteID = 10, Subindex = 0, TerritoryID = 135, Position = new Vector2(24.6f, 34.9f)}, //Moraby Drydocks
        //eastern la noscea 
        new AetheryteData() {AetheryteID = 11, Subindex = 0, TerritoryID = 137, Position = new Vector2(31.2f, 30.8f)}, //Costa del Sol
        new AetheryteData() {AetheryteID = 12, Subindex = 0, TerritoryID = 137, Position = new Vector2(21.1f, 21.5f)}, //Wineport
        //western la noscea
        new AetheryteData() {AetheryteID = 13, Subindex = 0, TerritoryID = 138, Position = new Vector2(34.5f, 31.7f)}, //Swiftperch
        new AetheryteData() {AetheryteID = 14, Subindex = 0, TerritoryID = 138, Position = new Vector2(26.6f, 25.8f)}, //Aleport
        //upper la noscea
        new AetheryteData() {AetheryteID = 15, Subindex = 0, TerritoryID = 139, Position = new Vector2(30.2f, 23.3f)}, //Camp Bronze Lake
        //outer la noscea
        new AetheryteData() {AetheryteID = 16, Subindex = 0, TerritoryID = 180, Position = new Vector2(19.1f, 17.2f)}, //Camp Overlook


        new AetheryteData() {AetheryteID = 2, Subindex = 0, TerritoryID = 132, Position = new Vector2(0)}, //New Gridania
        //central shroud
        new AetheryteData() {AetheryteID = 3, Subindex = 0, TerritoryID = 148, Position = new Vector2(21.7f, 22.2f)}, //Bentbranch Meadows
        //east shroud
        new AetheryteData() {AetheryteID = 4, Subindex = 0, TerritoryID = 152, Position = new Vector2(17.7f, 27.4f)}, //The Hawthorne Hut
        //south shroud
        new AetheryteData() {AetheryteID = 5, Subindex = 0, TerritoryID = 153, Position = new Vector2(25.0f, 20.1f)}, //Quarrymill
        new AetheryteData() {AetheryteID = 6, Subindex = 0, TerritoryID = 153, Position = new Vector2(16.8f, 28.6f)}, //Camp Tranquil
        //north shroud
        new AetheryteData() {AetheryteID = 7, Subindex = 0, TerritoryID = 154, Position = new Vector2(20.6f ,26.0f)}, //Fallgourd Float


        new AetheryteData()
            {AetheryteID = 9, Subindex = 0, TerritoryID = 130, Position = new Vector2(0)}, //Ul'dah - Steps of Nald
        //western thanalan
        new AetheryteData() {AetheryteID = 17, Subindex = 0, TerritoryID = 140, Position = new Vector2(22.8f, 16.9f)}, //Horizon
        //central thanalan
        new AetheryteData() {AetheryteID = 53, Subindex = 0, TerritoryID = 141, Position = new Vector2(21f, 18.1f)}, //Black Brush Station
        //eastern thanalan
        new AetheryteData() {AetheryteID = 18, Subindex = 0, TerritoryID = 145, Position = new Vector2(13.7f, 24.3f)}, //Camp Drybone
        //southern thanalan
        new AetheryteData() {AetheryteID = 19, Subindex = 0, TerritoryID = 146, Position = new Vector2(18.3f, 13.1f)}, //Little Ala Mhigo
        new AetheryteData() {AetheryteID = 20, Subindex = 0, TerritoryID = 146, Position = new Vector2(14.9f, 29.6f)}, //Forgotten Springs
        //northern thanalan
        new AetheryteData() {AetheryteID = 21, Subindex = 0, TerritoryID = 147, Position = new Vector2(21.9f, 30.5f)}, //Camp Bluefog
        new AetheryteData() {AetheryteID = 22, Subindex = 0, TerritoryID = 147, Position = new Vector2(20.9f, 20.9f)}, //Ceruleum Processing Plant

        //mor dhona
        new AetheryteData()
            {AetheryteID = 24, Subindex = 0, TerritoryID = 156, Position = new Vector2(22.2f, 8.1f)}, //Revenant's Toll

        //coerthas central highlands
        new AetheryteData()
            {AetheryteID = 23, Subindex = 0, TerritoryID = 155, Position = new Vector2(25.9f, 16.8f)}, //Camp Dragonhead
        #endregion

        #region Heavensward aetherytes

        //HW
        //coerthas western highlands
        new AetheryteData() { AetheryteID = 71, Subindex = 0, TerritoryID = 397, Position = new Vector2(32f, 36.7f)}, //Falcon's Nest

        //The sea of clouds
        new AetheryteData() { AetheryteID = 72, Subindex = 0, TerritoryID = 401, Position = new Vector2(10.3f, 33.6f)}, //Camp Cloudtop
        new AetheryteData() { AetheryteID = 73, Subindex = 0, TerritoryID = 401, Position = new Vector2(10.4f, 14.2f)}, //Ok' Zundu

        //azys lla
        new AetheryteData() { AetheryteID = 74, Subindex = 0, TerritoryID = 402, Position = new Vector2(8.1f, 10.6f)}, //Helix

        //idylshire / dravanian hinterlands
        new AetheryteData() { AetheryteID = 75, Subindex = 0, TerritoryID = 478, Position = new Vector2(0)}, //Idyllshire

        //the dravanian forelands
        new AetheryteData() { AetheryteID = 76, Subindex = 0, TerritoryID = 398, Position = new Vector2(33.2f, 23.1f)}, //Tailfeather
        new AetheryteData() { AetheryteID = 77, Subindex = 0, TerritoryID = 398, Position = new Vector2(16.4f, 23.2f)}, //Anyx Trine

        //the churning mists
        new AetheryteData() { AetheryteID = 78, Subindex = 0, TerritoryID = 400, Position = new Vector2(27.9f, 34.2f)}, //Moghome
        new AetheryteData() { AetheryteID = 79, Subindex = 0, TerritoryID = 400, Position = new Vector2(10.8f,28.8f)}, //Zenith

        #endregion

        #region Stormblood aetherytes
        //SB
        new AetheryteData() {AetheryteID = 104, Subindex = 0, TerritoryID = 635, Position = new Vector2(0)}, //Rhalgr's Reach
        //the fringe
        new AetheryteData() {AetheryteID = 98, Subindex = 0, TerritoryID = 612, Position = new Vector2(8.90f, 11.3f)}, //Castrum Oriens
        new AetheryteData() {AetheryteID = 99, Subindex = 0, TerritoryID = 612, Position = new Vector2(29.8f, 26.4f)}, //The Peering Stones

        //the peaks
        new AetheryteData() { AetheryteID = 100, Subindex = 0, TerritoryID = 620, Position = new Vector2(23.7f, 6.5f)}, //Ala Gannha
        new AetheryteData() { AetheryteID = 101, Subindex = 0, TerritoryID = 620, Position = new Vector2(16.0f ,36.4f)}, //Ala Ghiri

        //the lochs
        new AetheryteData() {AetheryteID = 102, Subindex = 0, TerritoryID = 621, Position = new Vector2(8.40f, 21.1f)}, //Porta Praetoria
        new AetheryteData() {AetheryteID = 103, Subindex = 0, TerritoryID = 621, Position = new Vector2(33.8f ,34.5f)}, //The Ala Mhigan Quarter

        //kugane
        new AetheryteData() {AetheryteID = 111, Subindex = 0, TerritoryID = 628, Position = new Vector2(0)}, //Kugane

        //the ruby sea
        new AetheryteData() {AetheryteID = 105, Subindex = 0, TerritoryID = 613, Position = new Vector2(28.6f, 16.2f)}, //Tamamizu
        new AetheryteData() {AetheryteID = 106, Subindex = 0, TerritoryID = 613, Position = new Vector2(23.2f, 9.8f)}, //Onokoro

        //yanxia
        new AetheryteData() {AetheryteID = 107, Subindex = 0, TerritoryID = 614, Position = new Vector2(30.1f, 19.6f)}, //Namai
        new AetheryteData() {AetheryteID = 108, Subindex = 0, TerritoryID = 614, Position = new Vector2(26.3f, 13.4f)}, //The House of the Fierce

        //the azim steppe
        new AetheryteData() {AetheryteID = 109, Subindex = 0, TerritoryID = 622, Position = new Vector2(32.5f, 28.3f)}, //Reunion
        new AetheryteData() {AetheryteID = 110, Subindex = 0, TerritoryID = 622, Position = new Vector2(23.0f, 22.1f)}, //The Dawn Throne
        new AetheryteData() {AetheryteID = 128, Subindex = 0, TerritoryID = 622, Position = new Vector2(6.30f, 23.8f)}, //Dhoro Iloh
        #endregion

        #region Shadowbringers aetherytes
        //ShB
        //lakeland
        new AetheryteData() {AetheryteID = 132, Subindex = 0, TerritoryID = 813, Position = new Vector2(36.5f, 20.9f)}, //Fort Jobb
        new AetheryteData() {AetheryteID = 136, Subindex = 0, TerritoryID = 813, Position = new Vector2(6.8f, 16.9f)}, //The Ostall Imperative

        //kholusia
        new AetheryteData() {AetheryteID = 137, Subindex = 0, TerritoryID = 814, Position = new Vector2()}, //Stilltide
        new AetheryteData() {AetheryteID = 138, Subindex = 0, TerritoryID = 814, Position = new Vector2()}, //Wright
        new AetheryteData() {AetheryteID = 139, Subindex = 0, TerritoryID = 814, Position = new Vector2()}, //Tomra

        //amh araeng
        new AetheryteData() {AetheryteID = 140, Subindex = 0, TerritoryID = 815, Position = new Vector2(26.4f, 17f)}, //Mord Souq
        new AetheryteData() {AetheryteID = 161, Subindex = 0, TerritoryID = 815, Position = new Vector2(29.4f, 27.6f)}, //The Inn at Journey's Head
        new AetheryteData() {AetheryteID = 141, Subindex = 0, TerritoryID = 815, Position = new Vector2(11.2f, 17.2f)}, //Twine

        //il mheg
        new AetheryteData() {AetheryteID = 144, Subindex = 0, TerritoryID = 816, Position = new Vector2(14.6f, 31.7f)}, //Lydha Lran
        new AetheryteData() {AetheryteID = 145, Subindex = 0, TerritoryID = 816, Position = new Vector2(20.0f, 4.3f)}, //Pla Enni
        new AetheryteData() {AetheryteID = 146, Subindex = 0, TerritoryID = 816, Position = new Vector2(29.1f, 7.7f)}, //Wolekdorf

        //the rak'tika greatwood lahee
        new AetheryteData()
            {AetheryteID = 142, Subindex = 0, TerritoryID = 817, Position = new Vector2(19.4f, 27.4f)}, //Slitherbough
        new AetheryteData() {AetheryteID = 143, Subindex = 0, TerritoryID = 817, Position = new Vector2(29.1f, 17.5f)}, //Fanow

        //the tempest
        new AetheryteData() {AetheryteID = 147, Subindex = 0, TerritoryID = 818, Position = new Vector2(32.7f, 17.5f)}, //The Ondo Cups
        //new AetheryteData() {AetheryteID = 148, Subindex = 0, TerritoryID = 818, Position = new Vector2()}, //The Macarenses Angle - super far away, don't tele here stupid
        #endregion

        #region Endwalker aetherytes
        //End Walker
        //thavnair
        new AetheryteData() {AetheryteID = 169, Subindex = 0, TerritoryID = 957, Position = new Vector2(25.4f,34f)}, //Yedlihmad
        new AetheryteData()
            {AetheryteID = 170, Subindex = 0, TerritoryID = 957, Position = new Vector2(10.9f,22.2f)}, //The Great Work
        new AetheryteData()
            {AetheryteID = 171, Subindex = 0, TerritoryID = 957, Position = new Vector2(29.5f ,16.5f)}, //Palaka's Stand

        //garlemald
        new AetheryteData()
            {AetheryteID = 172, Subindex = 0, TerritoryID = 958, Position = new Vector2(13.3f ,31f)}, //Camp Broken Glass
        new AetheryteData() {AetheryteID = 173, Subindex = 0, TerritoryID = 958, Position = new Vector2(31.8f,17.9f)}, //Tertium

        //labyrinthos
        new AetheryteData()
            {AetheryteID = 166, Subindex = 0, TerritoryID = 956, Position = new Vector2(30.3f, 11.9f)}, //The Archeion
        new AetheryteData()
            {AetheryteID = 167, Subindex = 0, TerritoryID = 956, Position = new Vector2(21.6f, 20.5f)}, //Sharlayan Hamlet
        new AetheryteData() {AetheryteID = 168, Subindex = 0, TerritoryID = 956, Position = new Vector2(6.9f, 27.5f)}, //Aporia

        //mare lamentorum
        new AetheryteData()
            {AetheryteID = 174, Subindex = 0, TerritoryID = 959, Position = new Vector2(10.1f,34.5f)}, //Sinus Lacrimarum
        //new AetheryteData() { AetheryteID = 175, Subindex = 0, TerritoryID = 959, Position = new Vector2()}, //Bestways Burrow, this is actually super far away and no-one should ever teleport here while hunting tbh imo tbf

        //ultima thule
        new AetheryteData() {AetheryteID = 179, Subindex = 0, TerritoryID = 960, Position = new Vector2(10.5f, 26.8f)}, //Reah Tahra
        new AetheryteData()
            {AetheryteID = 180, Subindex = 0, TerritoryID = 960, Position = new Vector2(22.6f , 8.3f)}, //Abode of the Ea
        new AetheryteData()
            {AetheryteID = 181, Subindex = 0, TerritoryID = 960, Position = new Vector2(31.2f, 28.1f)}, //Base Omicron

        //elpis

        new AetheryteData()
            {AetheryteID = 176, Subindex = 0, TerritoryID = 961, Position = new Vector2(24.6f, 24f)}, //Anagnorisis
        new AetheryteData()
            {AetheryteID = 177, Subindex = 0, TerritoryID = 961, Position = new Vector2(8.7f, 32.3f)}, //The Twelve Wonders
        new AetheryteData()
            {AetheryteID = 178, Subindex = 0, TerritoryID = 961, Position = new Vector2(10.8f, 17f)} //Poieten Oikos
        

        #endregion
    };

    private Telepo* _tele;


    public TeleportManager()=> _tele = Telepo.Instance();

    public void TeleportToHunt(HuntTrainMob mob)
    {
        if (!Aetherytes.Exists(a => a.TerritoryID == mob.TerritoryID)) return;
        var aeth = GetNearestAetheryte(mob.TerritoryID, mob.Position);
        _tele->Teleport(aeth.AetheryteID, aeth.Subindex);
    }

    private AetheryteData GetNearestAetheryte(uint territoryID, Vector2 mobPosition)
    {
        var zoneAetherytes = Aetherytes.Where(a => a.TerritoryID == territoryID).ToList();
        if (zoneAetherytes.Count() == 1) return zoneAetherytes[0];

        var aetheryte = zoneAetherytes[0];
        var smallestDist = Vector2.Distance(mobPosition, zoneAetherytes[0].Position);
        for (int i = 1; i < zoneAetherytes.Count; i++)
        {
            var tempDist = Vector2.Distance(mobPosition, zoneAetherytes[i].Position);
            if (tempDist < smallestDist)
            {
                smallestDist = tempDist;
                aetheryte = zoneAetherytes[i];
            }
        }
        return aetheryte;
    }
}