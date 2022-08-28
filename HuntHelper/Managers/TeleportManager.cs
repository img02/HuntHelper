using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HuntHelper.Managers.Hunts.Models;

namespace HuntHelper.Managers;

internal struct AetheryteData
{
    internal uint AetheryteID { get; init; }
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
            {AetheryteID = 8, Subindex = 0, TerritoryID = 129, Position = new Vector2()}, //Limsa Lominsa Lower Decks
        //middle la noscea
        new AetheryteData()
            {AetheryteID = 52, Subindex = 0, TerritoryID = 134, Position = new Vector2()}, //Summerford Farms
        //lower la noscea
        new AetheryteData()
            {AetheryteID = 10, Subindex = 0, TerritoryID = 135, Position = new Vector2()}, //Moraby Drydocks
        //eastern la noscea 
        new AetheryteData()
            {AetheryteID = 11, Subindex = 0, TerritoryID = 137, Position = new Vector2()}, //Costa del Sol
        new AetheryteData() {AetheryteID = 12, Subindex = 0, TerritoryID = 137, Position = new Vector2()}, //Wineport
        //western la noscea
        new AetheryteData() {AetheryteID = 13, Subindex = 0, TerritoryID = 138, Position = new Vector2()}, //Swiftperch
        new AetheryteData() {AetheryteID = 14, Subindex = 0, TerritoryID = 138, Position = new Vector2()}, //Aleport
        //upper la noscea
        new AetheryteData()
            {AetheryteID = 15, Subindex = 0, TerritoryID = 139, Position = new Vector2()}, //Camp Bronze Lake
        //outer la noscea
        new AetheryteData()
            {AetheryteID = 16, Subindex = 0, TerritoryID = 180, Position = new Vector2()}, //Camp Overlook


        new AetheryteData() {AetheryteID = 2, Subindex = 0, TerritoryID = 132, Position = new Vector2()}, //New Gridania
        //central shroud
        new AetheryteData()
            {AetheryteID = 3, Subindex = 0, TerritoryID = 148, Position = new Vector2()}, //Bentbranch Meadows
        //east shroud
        new AetheryteData()
            {AetheryteID = 4, Subindex = 0, TerritoryID = 152, Position = new Vector2()}, //The Hawthorne Hut
        //south shroud
        new AetheryteData() {AetheryteID = 5, Subindex = 0, TerritoryID = 153, Position = new Vector2()}, //Quarrymill
        new AetheryteData()
            {AetheryteID = 6, Subindex = 0, TerritoryID = 153, Position = new Vector2()}, //Camp Tranquil
        //north shroud
        new AetheryteData()
            {AetheryteID = 7, Subindex = 0, TerritoryID = 154, Position = new Vector2()}, //Fallgourd Float


        new AetheryteData()
            {AetheryteID = 9, Subindex = 0, TerritoryID = 130, Position = new Vector2()}, //Ul'dah - Steps of Nald
        //western thanalan
        new AetheryteData() {AetheryteID = 17, Subindex = 0, TerritoryID = 140, Position = new Vector2()}, //Horizon
        //central thanalan
        new AetheryteData()
            {AetheryteID = 53, Subindex = 0, TerritoryID = 141, Position = new Vector2()}, //Black Brush Station
        //eastern thanalan
        new AetheryteData()
            {AetheryteID = 18, Subindex = 0, TerritoryID = 145, Position = new Vector2()}, //Camp Drybone
        //southern thanalan
        new AetheryteData()
            {AetheryteID = 19, Subindex = 0, TerritoryID = 146, Position = new Vector2()}, //Little Ala Mhigo
        new AetheryteData()
            {AetheryteID = 20, Subindex = 0, TerritoryID = 146, Position = new Vector2()}, //Forgotten Springs
        //northern thanalan
        new AetheryteData()
            {AetheryteID = 21, Subindex = 0, TerritoryID = 147, Position = new Vector2()}, //Camp Bluefog
        new AetheryteData()
            {AetheryteID = 22, Subindex = 0, TerritoryID = 147, Position = new Vector2()}, //Ceruleum Processing Plant

        //mor dhona
        new AetheryteData()
            {AetheryteID = 24, Subindex = 0, TerritoryID = 156, Position = new Vector2()}, //Revenant's Toll

        //coerthas central highlands
        new AetheryteData()
            {AetheryteID = 23, Subindex = 0, TerritoryID = 155, Position = new Vector2()}, //Camp Dragonhead
        #endregion

        #region Heavensward aetherytes

        //HW
        //coerthas western highlands
        new AetheryteData()
            {AetheryteID = 71, Subindex = 0, TerritoryID = 397, Position = new Vector2()}, //Falcon's Nest

        //The sea of clouds
        new AetheryteData()
            {AetheryteID = 72, Subindex = 0, TerritoryID = 401, Position = new Vector2()}, //Camp Cloudtop
        new AetheryteData() {AetheryteID = 73, Subindex = 0, TerritoryID = 401, Position = new Vector2()}, //Ok' Zundu

        //azys lla
        new AetheryteData() {AetheryteID = 74, Subindex = 0, TerritoryID = 402, Position = new Vector2()}, //Helix

        //idylshire / dravanian hinterlands
        new AetheryteData() {AetheryteID = 75, Subindex = 0, TerritoryID = 478, Position = new Vector2()}, //Idyllshire

        //the dravanian forelands
        new AetheryteData() {AetheryteID = 76, Subindex = 0, TerritoryID = 398, Position = new Vector2()}, //Tailfeather
        new AetheryteData() {AetheryteID = 77, Subindex = 0, TerritoryID = 398, Position = new Vector2()}, //Anyx Trine

        //the churning mists
        new AetheryteData() {AetheryteID = 78, Subindex = 0, TerritoryID = 400, Position = new Vector2()}, //Moghome
        new AetheryteData() {AetheryteID = 79, Subindex = 0, TerritoryID = 400, Position = new Vector2()}, //Zenith

        #endregion

        #region Stormblood aetherytes
        //SB
        new AetheryteData()
            {AetheryteID = 104, Subindex = 0, TerritoryID = 635, Position = new Vector2()}, //Rhalgr's Reach
        //the fringe
        new AetheryteData()
            {AetheryteID = 98, Subindex = 0, TerritoryID = 612, Position = new Vector2()}, //Castrum Oriens
        new AetheryteData()
            {AetheryteID = 99, Subindex = 0, TerritoryID = 612, Position = new Vector2()}, //The Peering Stones

        //the peaks
        //new AetheryteData() { AetheryteID = 100, Subindex = 0, TerritoryID = 620, Position = new Vector2()}, //Ala Gannha
        new AetheryteData() {AetheryteID = 101, Subindex = 0, TerritoryID = 620, Position = new Vector2()}, //Ala Ghiri

        //the lochs
        new AetheryteData()
            {AetheryteID = 102, Subindex = 0, TerritoryID = 621, Position = new Vector2()}, //Porta Praetoria
        new AetheryteData()
            {AetheryteID = 103, Subindex = 0, TerritoryID = 621, Position = new Vector2()}, //The Ala Mhigan Quarter

        //kugane
        new AetheryteData() {AetheryteID = 111, Subindex = 0, TerritoryID = 628, Position = new Vector2()}, //Kugane

        //the ruby sea
        new AetheryteData() {AetheryteID = 105, Subindex = 0, TerritoryID = 613, Position = new Vector2()}, //Tamamizu
        new AetheryteData() {AetheryteID = 106, Subindex = 0, TerritoryID = 613, Position = new Vector2()}, //Onokoro

        //yanxia
        new AetheryteData() {AetheryteID = 107, Subindex = 0, TerritoryID = 614, Position = new Vector2()}, //Namai
        new AetheryteData()
            {AetheryteID = 108, Subindex = 0, TerritoryID = 614, Position = new Vector2()}, //The House of the Fierce

        //the azim steppe
        new AetheryteData() {AetheryteID = 109, Subindex = 0, TerritoryID = 622, Position = new Vector2()}, //Reunion
        new AetheryteData()
            {AetheryteID = 110, Subindex = 0, TerritoryID = 622, Position = new Vector2()}, //The Dawn Throne
        new AetheryteData() {AetheryteID = 128, Subindex = 0, TerritoryID = 622, Position = new Vector2()}, //Dhoro Iloh
        #endregion

        #region Shadowbringers aetherytes
        //ShB
        //lakeland
        new AetheryteData() {AetheryteID = 132, Subindex = 0, TerritoryID = 813, Position = new Vector2()}, //Fort Jobb
        new AetheryteData()
            {AetheryteID = 136, Subindex = 0, TerritoryID = 813, Position = new Vector2()}, //The Ostall Imperative

        //kholusia
        new AetheryteData() {AetheryteID = 137, Subindex = 0, TerritoryID = 814, Position = new Vector2()}, //Stilltide
        new AetheryteData() {AetheryteID = 138, Subindex = 0, TerritoryID = 814, Position = new Vector2()}, //Wright
        new AetheryteData() {AetheryteID = 139, Subindex = 0, TerritoryID = 814, Position = new Vector2()}, //Tomra

        //amh araeng
        new AetheryteData() {AetheryteID = 140, Subindex = 0, TerritoryID = 815, Position = new Vector2()}, //Mord Souq
        new AetheryteData()
            {AetheryteID = 161, Subindex = 0, TerritoryID = 815, Position = new Vector2()}, //The Inn at Journey's Head
        new AetheryteData() {AetheryteID = 141, Subindex = 0, TerritoryID = 815, Position = new Vector2()}, //Twine

        //il mheg
        new AetheryteData() {AetheryteID = 144, Subindex = 0, TerritoryID = 816, Position = new Vector2()}, //Lydha Lran
        new AetheryteData() {AetheryteID = 145, Subindex = 0, TerritoryID = 816, Position = new Vector2()}, //Pla Enni
        new AetheryteData() {AetheryteID = 146, Subindex = 0, TerritoryID = 816, Position = new Vector2()}, //Wolekdorf

        //the rak'tika greatwood lahee
        new AetheryteData()
            {AetheryteID = 142, Subindex = 0, TerritoryID = 817, Position = new Vector2()}, //Slitherbough
        new AetheryteData() {AetheryteID = 143, Subindex = 0, TerritoryID = 817, Position = new Vector2()}, //Fanow

        //the tempest
        new AetheryteData()
            {AetheryteID = 147, Subindex = 0, TerritoryID = 818, Position = new Vector2()}, //The Ondo Cups
        new AetheryteData()
            {AetheryteID = 148, Subindex = 0, TerritoryID = 818, Position = new Vector2()}, //The Macarenses Angle
        #endregion

        #region Endwalker aetherytes
        //End Walker
        //thavnair
        new AetheryteData() {AetheryteID = 169, Subindex = 0, TerritoryID = 957, Position = new Vector2()}, //Yedlihmad
        new AetheryteData()
            {AetheryteID = 170, Subindex = 0, TerritoryID = 957, Position = new Vector2()}, //The Great Work
        new AetheryteData()
            {AetheryteID = 171, Subindex = 0, TerritoryID = 957, Position = new Vector2()}, //Palaka's Stand

        //garlemald
        new AetheryteData()
            {AetheryteID = 172, Subindex = 0, TerritoryID = 958, Position = new Vector2()}, //Camp Broken Glass
        new AetheryteData() {AetheryteID = 173, Subindex = 0, TerritoryID = 958, Position = new Vector2()}, //Tertium

        //labyrinthos
        new AetheryteData()
            {AetheryteID = 166, Subindex = 0, TerritoryID = 956, Position = new Vector2()}, //The Archeion
        new AetheryteData()
            {AetheryteID = 167, Subindex = 0, TerritoryID = 956, Position = new Vector2()}, //Sharlayan Hamlet
        new AetheryteData() {AetheryteID = 168, Subindex = 0, TerritoryID = 956, Position = new Vector2()}, //Aporia

        //mare lamentorum
        new AetheryteData()
            {AetheryteID = 174, Subindex = 0, TerritoryID = 959, Position = new Vector2()}, //Sinus Lacrimarum
        //new AetheryteData() { AetheryteID = 175, Subindex = 0, TerritoryID = 959, Position = new Vector2()}, //Bestways Burrow, this is actually super far away and no-one should ever teleport here while hunting tbh imo tbf

        //ultima thule
        new AetheryteData() {AetheryteID = 179, Subindex = 0, TerritoryID = 960, Position = new Vector2()}, //Reah Tahra
        new AetheryteData()
            {AetheryteID = 180, Subindex = 0, TerritoryID = 960, Position = new Vector2()}, //Abode of the Ea
        new AetheryteData()
            {AetheryteID = 181, Subindex = 0, TerritoryID = 960, Position = new Vector2()}, //Base Omicron

        //elpis

        new AetheryteData()
            {AetheryteID = 176, Subindex = 0, TerritoryID = 961, Position = new Vector2()}, //Anagnorisis
        new AetheryteData()
            {AetheryteID = 177, Subindex = 0, TerritoryID = 961, Position = new Vector2()}, //The Twelve Wonders
        new AetheryteData()
            {AetheryteID = 178, Subindex = 0, TerritoryID = 961, Position = new Vector2()} //Poieten Oikos
        

        #endregion
    };

    private Telepo* _tele;


    public TeleportManager()
    {
        _tele = Telepo.Instance();

    }

    public void TeleportToHunt(HuntTrainMob mob)
    {
        var aeth = GetNearestAetheryte(mob.TerritoryID, mob.Position);
        _tele->Teleport(aeth.AetheryteID, aeth.Subindex);
    }

    private AetheryteData GetNearestAetheryte(uint territoryID, Vector2 mobPosition)
    {

        return Aetherytes[0];
    }
}