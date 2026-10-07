/*
   Copyright 2016-2022 LIPtoH <liptoh.codebase@gmail.com>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
using System;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using System.Data;
using Microsoft.Data.Sqlite;
using TS_SE_Tool.Storage;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Globalization;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;


using TS_SE_Tool.Utilities;
using TS_SE_Tool.Save.Items;

namespace TS_SE_Tool
{
    public partial class FormMain : Form
    {
        private bool NewPrepareData()
        {
            IO_Utilities.LogWriter("Prepare started");
            UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Info, "message_preparing_data");

            SiiNunitData = new SiiNunit(tempSavefileInMemory);

            if (SiiNunitData == null)
            {
                return false;
            }

            ExtraPrepareStuff();

            UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Info, "message_operation_finished");
            IO_Utilities.LogWriter("Prepare ended");

            return true;
        }

        private void ExtraPrepareStuff()
        {
            workerLoadSaveFile.ReportProgress(80);

            //namelessList.Sort();
            //namelessList = namelessList.Distinct().ToList();

            PreparePlayerDictionariesInitial();
            PrepareCitiesInitial();
            PrepareGaragesInitial();
            PrepareVisitedCitiesInitial();
            PrepareGPSInitial();
            PrepareCargoTrailerDefsVariantsLists();

            //ExportnamelessList();

            //Exclude company from city if no jobs assigned by game
            foreach (City city in CitiesList)
            {
                city.ExcludeCompany();
                if (VisitedCities.Exists(x => x.Name == city.CityName))
                    city.Visited = true;
            }

            //LoadAdditionalCargo(); //REWRITE

            CompaniesList = CompaniesList.Distinct().ToList();//Delete duplicates
            CargoesList = CargoesList.Distinct().ToList(); //Delete duplicates

            CompanyTruckComparer companyTruckComparer = new CompanyTruckComparer();

            CompanyTruckList = CompanyTruckList.Distinct(companyTruckComparer).ToList(); //Delete duplicates
            HeavyCargoList = HeavyCargoList.Distinct().ToList(); //Delete duplicates

            //Set country to city
            foreach (City tempcity in CitiesList)
            {
                string country = CountryDictionary.GetCountry(tempcity.CityName);
                tempcity.Country = country;

                if ((country != null) && (country != ""))
                {
                    CountriesList.Add(country);
                }
            }

            CountriesList = CountriesList.Distinct().ToList();

            CountryDictionary.SaveDictionaryFile(); //Save country-city list to file

            //Filter garages
            foreach (City tempcity in from x in CitiesList where !x.Disabled select x)
            {
                Garages tmpgrg = GaragesList.Find(x => x.GarageName == tempcity.CityName);
                if (tmpgrg != null)
                    tmpgrg.IgnoreStatus = false;
            }

            GaragesList = GaragesList.Distinct().OrderBy(x => x.GarageName).ToList();

            PrepareDBdata();

            //Output new data for translation
            SaveCompaniesLng();
            SaveCitiesLng();
            SaveCargoLng();

            //GetCompaniesCargoInOut();
            workerLoadSaveFile.ReportProgress(90);

            GetAllDistancesFromDB();

            workerLoadSaveFile.ReportProgress(100);
        }

        private void CheckSaveInfoData()
        {
            MainSaveFileInfoData.ProcessData(tempInfoFileInMemory);

            if (MainSaveFileInfoData.Version > 0)
            {
                if (MainSaveFileInfoData.Version > SupportedSavefileVersionETS2[1])
                {
                    string dialogCaption = "", dialogText = "";
                    string[] returnValues = HelpTranslateDialog("UnsupportedVersion");

                    dialogText = Regex.Unescape(String.Format(returnValues[1], MainSaveFileInfoData.Version));

                    DialogResult DR = UpdateStatusBarMessage.ShowMessageBox(this, dialogText, returnValues[0], MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (DR == DialogResult.No)
                    {
                        InfoDepContinue = false;
                        return;
                    }
                }

                if (MainSaveFileInfoData.Version < SupportedSavefileVersionETS2[0])
                {
                    string dialogCaption = "", dialogText = "";
                    string[] returnValues = HelpTranslateDialog("NoBackwardCompatibility");

                    dialogText = Regex.Unescape(String.Format(returnValues[1], MainSaveFileInfoData.Version));

                    DialogResult DR = UpdateStatusBarMessage.ShowMessageBox(this, dialogText, returnValues[0], MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (DR == DialogResult.OK)
                    {
                        InfoDepContinue = false;
                        return;
                    }
                }
            }
            else if (MainSaveFileInfoData.Version == 0)
            {
                DialogResult result = UpdateStatusBarMessage.ShowMessageBox(this, "Savefile version was not recognised." + Environment.NewLine + "Do you want to continue?", "Version not recognised", MessageBoxButtons.YesNo);

                if (result == DialogResult.No)
                {
                    InfoDepContinue = false;
                    return;
                }
            }

            string sql = "UPDATE [DatabaseDetails] SET SaveVersion = " + MainSaveFileInfoData.Version + " WHERE ID_DBline = 1;";
            UpdateDatabase(sql);

            GetDataFromDatabase("Dependencies");

            //Check dependencies
            if (DBDependencies.Count == 0)
            {
                InsertDataIntoDatabase("Dependencies");
                InfoDepContinue = true;
            }
            else
            {
                List<string> tmpSFdep = MainSaveFileInfoData.Dependencies.Where(x => x.RawDepType != "rdlc").Select(x => x.Raw.Value).ToList();

                List<string> dbdep = DBDependencies.Except(tmpSFdep).ToList();
                List<string> sfdep = tmpSFdep.Except(DBDependencies).ToList();

                if (dbdep.Count > 0 || sfdep.Count > 0)
                {
                    string dbdepstr = "", sfdepstr = "";

                    if (dbdep.Count > 0)
                    {
                        dbdepstr += "\r\nDependencies only in Database (" + dbdep.Count.ToString() + ") will be Deleted:\r\n";
                        int i = 0;
                        foreach (string temp in dbdep)
                        {
                            i++;
                            dbdepstr += i.ToString() + ") " + temp + "\r\n";
                        }
                    }

                    if (sfdep.Count > 0)
                    {
                        sfdepstr += "\r\nDependencies only in Save file (" + sfdep.Count.ToString() + ") will be Added:\r\n";
                        int i = 0;
                        foreach (string temp in sfdep)
                        {
                            i++;
                            sfdepstr += i.ToString() + ") " + temp + "\r\n"; ;
                        }
                    }

                    DialogResult r = UpdateStatusBarMessage.ShowMessageBox(this,
                        "Save file and Database has different Dependencies due to installed\\deleted mods\\dlc's." + Environment.NewLine +
                        "This may result in wrong path and cargo data." + Environment.NewLine + Environment.NewLine +
                        "Do you want to Proceed and Update Dependencies?" + Environment.NewLine +
                        dbdepstr + Environment.NewLine + sfdepstr, "Dependencies conflict",
                        MessageBoxButtons.YesNo);

                    if (r == DialogResult.Yes)
                    {
                        //Update Dependencies
                        InsertDataIntoDatabase("Dependencies");
                        InfoDepContinue = true;
                    }
                    else
                    {
                        //Stop opening save
                        InfoDepContinue = false;
                    }
                }
                else
                {
                    InfoDepContinue = true;
                }
            }

            if (!InfoDepContinue)
                return;

            LoadCachedExternalCargoData("def");

            if (MainSaveFileInfoData.Dependencies.Count > 0)
                foreach (Dependency tDepend in MainSaveFileInfoData.Dependencies)
                {
                    LoadCachedExternalCargoData(tDepend.DepLoadID);
                }

            if (MainSaveFileInfoData.Version == 0)
                UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Error, "error_save_version_not_detected");
        }

        public string GetCustomSaveFilename(string _tempSaveFilePath)
        {
            string chunkOfline;

            string tempSiiInfoPath = _tempSaveFilePath + @"\info.sii";
            string[] tempFile = null;

            if (!File.Exists(tempSiiInfoPath))
            {
                IO_Utilities.LogWriter("File does not exist in " + tempSiiInfoPath);
                UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Error, "error_could_not_find_file");
            }
            else
            {
                FileDecoded = false;
                try
                {
                    int decodeAttempt = 0;
                    while (decodeAttempt < 5)
                    {
                        tempFile = NewDecodeFile(tempSiiInfoPath, false);

                        if (FileDecoded)
                        {
                            break;
                        }

                        decodeAttempt++;
                    }

                    if (decodeAttempt == 5)
                    {
                        IO_Utilities.LogWriter("Could not decrypt after 5 attempts");
                        UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Error, "error_could_not_decode_file");
                    }
                }
                catch
                {
                    IO_Utilities.LogWriter("Could not read: " + tempSiiInfoPath);
                }

                if ((tempFile == null) || (tempFile[0] != "SiiNunit"))
                {
                    IO_Utilities.LogWriter("Wrongly decoded Info file or wrong file format");
                    UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Error, "error_file_not_decoded");
                }
                else if (tempFile != null)
                {
                    for (int line = 0; line < tempFile.Length; line++)
                    {
                        if (tempFile[line].StartsWith(" name:"))
                        {
                            chunkOfline = tempFile[line];
                            string CustomName = chunkOfline.Split(new char[] { ' ' }, 3)[2];

                            if (CustomName.StartsWith("\""))
                            {
                                CustomName = CustomName.Substring(1, CustomName.Length - 2);
                            }

                            return CustomName;
                        }
                    }
                }
            }
            //////
            return "<!>Error<!>";
        }

        //Remove broken color sets
        private void PrepareUserColors()
        {
            if (MainSaveFileInfoData.Version < 49)
                return;

            int setcount = SiiNunitData.Economy.user_colors.Count() / 4;

            //iterate through sets
            for (int i = 0; i < setcount; i++)
            {
                if (SiiNunitData.Economy.user_colors[4 * i].color.A == 0)
                {
                    //clear color set
                    for (int j = 1; j < 4; j++)
                    {
                        SiiNunitData.Economy.user_colors[4 * i + j] = new Save.DataFormat.SCS_Color(0, 0, 0, 0);
                    }
                    continue;
                }
            }

            RemoveUserColorUnused4slot();
        }

        //
        private void PrepareCitiesInitial()
        {
            string[] chunks;

            foreach (string company in SiiNunitData.Economy.companies)
            {
                chunks = company.Split(new char[] { '.' });

                string cityname = chunks[3], companyname = chunks[2];

                if (cityname == null)
                    continue;

                //Add City to List from companies list
                if (CitiesList.Where(x => x.CityName == cityname).Count() == 0)
                {
                    CitiesList.Add(new City(cityname));
                }

                CompaniesList.Add(companyname); //add company to list

                //Add Company to City from companies list                            
                foreach (City tempcity in CitiesList.FindAll(x => x.CityName == cityname))
                {
                    tempcity.AddCompany(companyname);

                    Save.Items.Company tempcompany = (Save.Items.Company)SiiNunitData.SiiNitems[company];

                    tempcity.UpdateCompanyJobOffersCount(companyname, tempcompany.job_offer.Count);

                    tempcity.UpdateCompanyCargoSeeds(companyname, tempcompany.cargo_offer_seeds.ToArray());
                }
            }
        }

        private void PrepareGaragesInitial()
        {
            foreach (string garage in SiiNunitData.Economy.garages)
            {
                string garageName = garage.Split(new char[] { '.' })[1];

                Save.Items.Garage tmpSiiNGarage = SiiNunitData.SiiNitems[garage];

                GaragesList.Add(new Garages(garageName, tmpSiiNGarage.status));

                Garages tempGarage = GaragesList.Find(x => x.GarageName == garageName);

                tempGarage.Vehicles.AddRange(tmpSiiNGarage.vehicles);
                tempGarage.Drivers.AddRange(tmpSiiNGarage.drivers);
                tempGarage.Trailers.AddRange(tmpSiiNGarage.trailers);
            }
        }

        private void PrepareVisitedCitiesInitial()
        {
            int cityid = 0;
            foreach (string city in SiiNunitData.Economy.visited_cities)
            {
                VisitedCities.Add(new VisitedCity(city, SiiNunitData.Economy.visited_cities_count[cityid], true));
                cityid++;
            }
        }

        private void PreparePlayerDictionariesInitial()
        {
            foreach (string trck in SiiNunitData.Player.trucks)
            {
                UserTruckDictionary.Add(trck, new UserCompanyTruckData());

                UserTruckDictionary[trck].TruckMainData = SiiNunitData.SiiNitems[trck];
            }

            //
            foreach (string trlr in SiiNunitData.Player.trailers)
            {
                UserTrailerDictionary.Add(trlr, new UserCompanyTrailerData());
                UserTrailerDictionary[trlr].TrailerMainData = SiiNunitData.SiiNitems[trlr];
            }

            //
            foreach (string trlrDef in SiiNunitData.Player.trailer_defs)
            {
                UserTrailerDefDictionary.Add(trlrDef, SiiNunitData.SiiNitems[trlrDef]);
            }

            //
            if (SiiNunitData.Player_Job != null)
            {
                string jobtrck = SiiNunitData.Player_Job.company_truck;
                if (jobtrck != "null")
                {
                    UserTruckDictionary.Add(jobtrck, new UserCompanyTruckData());
                    UserTruckDictionary[jobtrck].TruckMainData = SiiNunitData.SiiNitems[jobtrck];
                    UserTruckDictionary[jobtrck].Users = false;
                }

                string jobtrlr = SiiNunitData.Player_Job.company_trailer;
                if (jobtrlr != "null")
                {
                    UserTrailerDictionary.Add(jobtrlr, new UserCompanyTrailerData());
                    UserTrailerDictionary[jobtrlr].TrailerMainData = SiiNunitData.SiiNitems[jobtrlr];
                    UserTrailerDictionary[jobtrlr].Users = false;
                }
            }

            //
            for(int i = 0; i < SiiNunitData.Player.drivers.Count; i++)
            {
                UserCompanyDriverData DrData = new UserCompanyDriverData();
                string drvr = SiiNunitData.Player.drivers[i];

                if (i == 0)
                {
                    Save.Items.Player dr = (Save.Items.Player)SiiNunitData.Player;

                    DrData.AssignedTruck = dr.assigned_truck;
                    DrData.AssignedTrailer = dr.assigned_trailer;
                }
                else
                {
                    Save.Items.Driver_AI dr = (Save.Items.Driver_AI)SiiNunitData.SiiNitems[drvr];

                    DrData.AssignedTruck = dr.assigned_truck;
                    DrData.AssignedTrailer = dr.assigned_trailer;
                }

                UserDriverDictionary.Add(drvr, DrData);
            }
        }

        private void PrepareGPSInitial()
        {
            //GPS
            //Online
            foreach (string entry in SiiNunitData.Economy.stored_online_gps_behind_waypoints)
            {
                GPSbehindOnline.Add(entry, new List<string>());
            }

            foreach (string entry in SiiNunitData.Economy.stored_online_gps_ahead_waypoints)
            {
                GPSaheadOnline.Add(entry, new List<string>());
            }

            //Offline
            //Normal
            foreach (string entry in SiiNunitData.Economy.stored_gps_behind_waypoints)
            {
                GPSbehind.Add(entry, new List<string>());
            }

            foreach (string entry in SiiNunitData.Economy.stored_gps_ahead_waypoints)
            {
                GPSahead.Add(entry, new List<string>());
            }
            //Avoid
            foreach (string entry in SiiNunitData.Economy.stored_gps_avoid_waypoints)
            {
                GPSAvoid.Add(entry, new List<string>());
            }
        }

        private void PrepareVisitedCitiesWrite()
        {
            foreach (City city in CitiesList)
            {
                VisitedCity temp = VisitedCities.Find(x => x.Name == city.CityName);

                if (temp != null)
                {
                    if (!city.Visited)
                        VisitedCities[VisitedCities.IndexOf(temp)].VisitCount = 0;
                }
                else
                {
                    if (city.Visited)
                        VisitedCities.Add(new VisitedCity(city.CityName, 1, true));
                    else
                        VisitedCities.Add(new VisitedCity(city.CityName, 0, false));
                }
            }

            foreach (VisitedCity vc in VisitedCities)
            {
                if (vc.Visited)
                {
                    if (!SiiNunitData.Economy.visited_cities.Contains(vc.Name))
                    {
                        SiiNunitData.Economy.visited_cities.Add(vc.Name);
                        SiiNunitData.Economy.visited_cities_count.Add(vc.VisitCount);
                    }
                }
                else
                {
                    if (SiiNunitData.Economy.visited_cities.Contains(vc.Name))
                    {
                        int idx = SiiNunitData.Economy.visited_cities.IndexOf(vc.Name);

                        SiiNunitData.Economy.visited_cities.RemoveAt(idx);
                        SiiNunitData.Economy.visited_cities_count.RemoveAt(idx);
                    }
                }
            }

        }

        private void PrepareCargoTrailerDefsVariantsLists()
        {
            foreach (string company in SiiNunitData.Economy.companies)
            {
                Save.Items.Company tmpCompany = SiiNunitData.SiiNitems[company];

                //
                int cargotype = 0, units_count = 0;
                string cargo = "", trailervariant = "", trailerdefinition = "", company_truck = "";

                foreach (string job_offer in tmpCompany.job_offer)
                {
                    Save.Items.Job_offer_Data tmpJob_offer_Data = SiiNunitData.SiiNitems[job_offer];

                    if (tmpJob_offer_Data.cargo == "null")
                        continue;

                    //===
                    company_truck = tmpJob_offer_Data.company_truck;

                    cargo = tmpJob_offer_Data.cargo.Split(new char[] { '.' })[1];
                    trailervariant = tmpJob_offer_Data.trailer_variant;
                    trailerdefinition = tmpJob_offer_Data.trailer_definition;

                    units_count = tmpJob_offer_Data.units_count;

                    //===

                    cargotype = 0;

                    //===

                    if (company_truck.Contains("\"heavy"))
                    {
                        cargotype = 1;
                    }
                    else if (company_truck.Contains("\"double"))
                    {
                        cargotype = 2;
                    }

                    //===

                    CompanyTruckList.Add(new CompanyTruck(company_truck, cargotype));

                    //===

                    if (!TrailerVariants.Contains(trailervariant))
                        TrailerVariants.Add(trailervariant);

                    //===


                    Cargo tempCargo = CargoesList.Find(x => x.CargoName == cargo);

                    if (tempCargo == null)
                    {
                        CargoesList.Add(new Cargo(cargo, cargotype, trailerdefinition, units_count));
                    }
                    else
                    {
                        List<TrailerDefinition> tmpTDlist = tempCargo.TrailerDefList;

                        if (!tmpTDlist.Exists(x => x.DefName == trailerdefinition && x.CargoType == cargotype))
                        {
                            tmpTDlist.Add(new TrailerDefinition(trailerdefinition, cargotype, units_count));
                        }
                        else
                        {
                            TrailerDefinition tmpTDitem = tmpTDlist.Find(x => x.DefName == trailerdefinition && x.CargoType == cargotype);

                            if (!tmpTDitem.CargoLoadVariants.Exists(x => x.UnitsCount == units_count)) 
                            {
                                tmpTDitem.CargoLoadVariants.Add(new CargoLoadVariants(units_count));
                            }
                        }
                    }

                    if (!TrailerDefinitionVariants.ContainsKey(trailerdefinition))
                    {
                        List<string> tmp = new List<string> { trailervariant };
                        TrailerDefinitionVariants.Add(trailerdefinition, tmp);
                    }
                    else
                    {
                        if (!TrailerDefinitionVariants[trailerdefinition].Contains(trailervariant))
                        {
                            TrailerDefinitionVariants[trailerdefinition].Add(trailervariant);
                        }
                    }
                    //===
                }
            }
        }

        private void PrepareDBdata()
        {
            // Get Data From Database

            GetDataFromDatabase("CargoesTable");
            GetDataFromDatabase("CitysTable");
            GetDataFromDatabase("CompaniesTable");
            GetDataFromDatabase("TrucksTable");

            InsertDataIntoDatabase("CitysTable");
            InsertDataIntoDatabase("CompaniesTable");
            InsertDataIntoDatabase("TrucksTable");
            InsertDataIntoDatabase("TrailerTables");
            
            InsertDataIntoDatabase("CargoesTable");

            InsertDataIntoDatabase("DistancesTable");

            // SQLite reuses free pages; do not VACUUM on every save or block the UI.
            SqliteStorage.Execute(DBconnection, "PRAGMA optimize");
            DBconnection.Close();
        }

        //Apply new garage size and Copy extra items to temp Lists
        private void PrepareGarages()
        {
            List<string> extraTrailers = new List<string>();

            foreach (Garages tempGarage in GaragesList)
            {
                int capacity = 0;

                switch (tempGarage.GarageStatus)
                {
                    case 2:
                        {
                            capacity = 3;
                            break;
                        }
                    case 3:
                        {
                            capacity = 5;
                            break;
                        }
                    case 6:
                        {
                            capacity = 1;
                            break;
                        }
                }

                if (capacity == 0)
                {
                    //Move
                    extraVehicles.AddRange(tempGarage.Vehicles);
                    extraDrivers.AddRange(tempGarage.Drivers);
                    extraTrailers.AddRange(tempGarage.Trailers);

                    //Delete
                    tempGarage.Vehicles.Clear();
                    tempGarage.Drivers.Clear();
                    tempGarage.Trailers.Clear();
                }
                else
                {
                    int cur = tempGarage.Vehicles.Count;

                    if (capacity < cur)
                    {
                        extraVehicles.AddRange(tempGarage.Vehicles.GetRange(capacity, cur - capacity));
                        extraDrivers.AddRange(tempGarage.Drivers.GetRange(capacity, cur - capacity));

                        tempGarage.Vehicles.RemoveRange(capacity, cur - capacity);
                        tempGarage.Drivers.RemoveRange(capacity, cur - capacity);
                    }
                    else if (capacity > cur)
                    {
                        string rstr = null;
                        tempGarage.Vehicles.AddRange(Enumerable.Repeat(rstr, capacity - cur));
                        tempGarage.Drivers.AddRange(Enumerable.Repeat(rstr, capacity - cur));
                    }
                }
            }

            //Move extra trailers to HQ garage
            if (extraTrailers.Count > 0)
            {
                GaragesList[GaragesList.FindIndex(x => x.GarageName == SiiNunitData.Player.hq_city)].Trailers.AddRange(extraTrailers);
                extraTrailers.Clear();
            }

            //Remove empty records from lists
            int iV = extraDrivers.Count();

            for (int i = iV - 1; i >= 0; i--)
            {
                if (extraVehicles[i] == extraDrivers[i])
                {
                    extraVehicles.RemoveAt(i);
                    extraDrivers.RemoveAt(i);
                }
            }

            //Unallocated Drivers
            if (extraDrivers.Count() > 0)
            {
                if (extraDrivers.Contains(SiiNunitData.Player.drivers[0]))
                {
                    Garages tmpG = new Garages(SiiNunitData.Player.hq_city);

                    int hqIdx = GaragesList.IndexOf(tmpG);
                    int sIdx = 0;

                    int DrvIdx, VhcIdx;

                    while (true)
                    {
                        DrvIdx = GaragesList[hqIdx].Drivers.FindIndex(sIdx, x => x == null);
                        VhcIdx = GaragesList[hqIdx].Vehicles.FindIndex(sIdx, x => x == null);
                        
                        if (DrvIdx > -1 && VhcIdx > -1)
                        {
                            if (DrvIdx == VhcIdx)
                            {
                                break;
                            }
                            else
                            {
                                if (DrvIdx > VhcIdx)
                                    sIdx = DrvIdx;
                                else
                                    sIdx = VhcIdx;
                            }
                        }
                        else
                        {
                            DrvIdx = 0;
                            break;
                        }
                    }

                    extraDrivers.Add(GaragesList[hqIdx].Drivers[DrvIdx]);
                    extraVehicles.Add(GaragesList[hqIdx].Vehicles[DrvIdx]);

                    int tmpIdx = extraDrivers.IndexOf(SiiNunitData.Player.drivers[0]);

                    GaragesList[hqIdx].Drivers[DrvIdx] = extraDrivers[tmpIdx];
                    GaragesList[hqIdx].Vehicles[DrvIdx] = extraVehicles[tmpIdx];

                    extraDrivers.RemoveAt(tmpIdx);
                    extraVehicles.RemoveAt(tmpIdx);
                }
            }
        }

        private void PrepareGaragesWrite()
        {
            foreach (string grg in SiiNunitData.Economy.garages)
            {
                Save.Items.Garage siiGarage = SiiNunitData.SiiNitems[grg];
                Garages prgrGarage = GaragesList.Find(x => x.GarageName == grg.Split(new char[] { '.' })[1]);

                siiGarage.drivers = prgrGarage.Drivers;
                siiGarage.vehicles = prgrGarage.Vehicles;
                siiGarage.trailers = prgrGarage.Trailers;
                siiGarage.status = prgrGarage.GarageStatus;
            }
        }

        private void PrepareCompaniesJobWrite()
        {
            foreach (KeyValuePair<string, List<JobAdded>> cmp in AddedJobsDictionary)
            {
                Save.Items.Company siiCompany = SiiNunitData.SiiNitems[cmp.Key];

                for (int i = 0; i < cmp.Value.Count; i++)
                {
                    JobAdded job = cmp.Value.ElementAt(i);

                    string jobId = siiCompany.job_offer[i];

                    Save.Items.Job_offer_Data siiJob = SiiNunitData.SiiNitems[jobId];

                    siiJob.target = "\"" + job.DestinationCompany + "." + job.DestinationCity + "\"";
                    siiJob.expiration_time = job.ExpirationTime;
                    siiJob.urgency = job.Urgency;
                    siiJob.shortest_distance_km = job.Distance;
                    siiJob.ferry_time = job.Ferrytime;
                    siiJob.ferry_price = job.Ferryprice;
                    siiJob.cargo = "cargo." + job.Cargo;
                    siiJob.company_truck = job.CompanyTruck;
                    siiJob.trailer_variant = job.TrailerVariant;
                    siiJob.trailer_definition = job.TrailerDefinition;
                    siiJob.units_count = job.UnitsCount;
                    //siiJob.fill_ratio = 1;
                }
            }
        }

        //Rearrange extra User Drivers to glogal Driver pool
        private void PrepareDriversTrucksWrite()
        {
            extraDrivers.RemoveAll(x => x == null);

            foreach (string tmp in extraDrivers)
            {
                if (tmp != null)
                {
                    int idx = 0;

                    idx = SiiNunitData.Player.drivers.IndexOf(tmp);

                    SiiNunitData.Economy.driver_pool.Add(tmp);

                    SiiNunitData.Player.drivers.RemoveAt(idx);
                    SiiNunitData.Player.driver_readiness_timer.RemoveAt(idx);
                    SiiNunitData.Player.driver_quit_warned.RemoveAt(idx);

                    ((Save.Items.Driver_AI)SiiNunitData.SiiNitems[tmp]).SetForDriverPool();
                }
            }

            extraVehicles.RemoveAll(x => x == null);

            foreach (string tmp in extraVehicles)
            {
                int idx = 0;

                idx = SiiNunitData.Player.trucks.IndexOf(tmp);

                SiiNunitData.NamelessIgnoreList.Add(tmp);
                SiiNunitData.Player.trucks.RemoveAt(idx);

                SiiNunitData.NamelessIgnoreList.Add(SiiNunitData.Player.truck_profit_logs[idx]);
                SiiNunitData.Player.truck_profit_logs.RemoveAt(idx);

            }

            //Check hired drivers
            foreach (string grgNameless in SiiNunitData.Economy.garages)
            {
                Save.Items.Garage grg = SiiNunitData.SiiNitems[grgNameless];

                foreach (string drvrNameless in grg.drivers)
                {
                    if (drvrNameless != null && drvrNameless != SiiNunitData.Player.drivers[0])
                    {
                        string grgName = grgNameless.Split('.')[1];
                        Driver_AI drvr = SiiNunitData.SiiNitems[drvrNameless];

                        if (String.IsNullOrEmpty(drvr.hometown.Value))
                        {
                            drvr.hometown = grgName;
                            drvr.current_city = grgName;
                            drvr.training_policy = 1;

                            Economy_event ecEvent = new Economy_event(SiiNunitData.Economy.game_time, drvrNameless, 3);

                            string spareNameless = GetSpareNameless();

                            SiiNunitData.Economy_event_Queue.data.Add(spareNameless);
                            SiiNunitData.SiiNitems.Add(spareNameless, ecEvent);
                        }
                        else
                        {
                            drvr.hometown = grgName;
                        }
                    }
                }
            }
        }

        //Sort events by time
        private void PrepareEvents()
        {
            Dictionary<string, Economy_event> timeList = new Dictionary<string, Economy_event>();

            foreach (string ecEventLink in SiiNunitData.Economy_event_Queue.data)
            {
                Economy_event ecEvent = ((Economy_event)SiiNunitData.SiiNitems[ecEventLink]);
                string cmpLink = ecEvent.unit_link;

                if (AddedJobsDictionary.ContainsKey(cmpLink))
                {
                    if (ecEvent.param < AddedJobsDictionary[cmpLink].Count)
                    {
                        ecEvent.time = AddedJobsDictionary[cmpLink].ElementAt(ecEvent.param).ExpirationTime;
                    }
                }

                timeList.Add(ecEventLink, ecEvent);
            }

            //Sort by time
            var sortedDict = from entry in timeList orderby entry.Value.time ascending select entry;

            List<string> newQueue = new List<string>();
            newQueue.AddRange(sortedDict.Select(x => x.Key));

            SiiNunitData.Economy_event_Queue.data = newQueue;
            
        }

        // The legacy .sdf is imported read-only on first use; new data lives in .sqlite.
        private void CreateDatabase(string fileName)
        {
            SqliteStorage.Ensure(fileName, DatabaseKind.Profile, GameType, Globals.SelectedProfile,
                TextUtilities.FromHexToString(Globals.SelectedProfile));
        }

        private void UpdateDatabase(string sql)
        {
            try { SqliteStorage.Execute(DBconnection, sql); }
            finally { DBconnection.Close(); }
        }

        //Load distances from database
        private void GetAllDistancesFromDB()
        {
            try
            {
                RouteList.ClearList(); //Clears existing list in program

                DBconnection.Open();

                string commandText = "SELECT SourceCity.CityName AS SourceCityName, SourceCompany.CompanyName AS SourceCompanyName, DestinationCity.CityName AS DestinationCityName, " +
                    "DestinationCompany.CompanyName AS DestinationCompanyName, Distance, FerryTime, FerryPrice " +
                    "FROM DistancesTable " +
                    "INNER JOIN CompaniesTable AS DestinationCompany ON DistancesTable.DestinationCompanyID = DestinationCompany.ID_company " +
                    "INNER JOIN CitysTable AS DestinationCity ON DistancesTable.DestinationCityID = DestinationCity.ID_city " +
                    "INNER JOIN CompaniesTable AS SourceCompany ON DistancesTable.SourceCompanyID = SourceCompany.ID_company " +
                    "INNER JOIN CitysTable AS SourceCity ON DistancesTable.SourceCityID = SourceCity.ID_city;";

                SqliteDataReader reader = new SqliteCommand(commandText, DBconnection).ExecuteReader();

                while (reader.Read())
                {
                    RouteList.AddRoute(reader["SourceCityName"].ToString(), reader["SourceCompanyName"].ToString(), reader["DestinationCityName"].ToString(), reader["DestinationCompanyName"].ToString(),
                        reader["Distance"].ToString(), reader["FerryTime"].ToString(), reader["FerryPrice"].ToString());
                }

                DBconnection.Close();
            }
            catch (SqliteException sqlexception)
            {
                UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Error, "error_sql_exception");
                MessageBox.Show(sqlexception.Message, "SQL Exception. Load all Distances", MessageBoxButtons.OK, MessageBoxIcon.Error);

                IO_Utilities.LogWriter("Getting Data went wrong");
            }
            catch (Exception ex)
            {
                UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Error, "error_exception");
                MessageBox.Show(ex.Message, "Exception.", MessageBoxButtons.OK, MessageBoxIcon.Error);

                IO_Utilities.LogWriter("Getting Data went wrong");
            }

            IO_Utilities.LogWriter("Loaded " + RouteList.CountItems() + " routes from DataBase");
        }

        private void InsertNames(string table, string column, IEnumerable<string> names)
        {
            using (DataTable rows = new DataTable())
            {
                rows.Columns.Add(column, typeof(string));
                foreach (string name in names.Distinct()) rows.Rows.Add(name);
                SqliteStorage.WriteRows(DBconnection, table, rows, true);
            }
        }

        // Parameterized row inserts avoid SQL injection and SQLite's 500-term UNION limit.
        private void InsertDataIntoDatabase(string target)
        {
            DBconnection.Open();
            try
            {
                switch (target)
                {
                    case "Dependencies":
                        if (MainSaveFileInfoData.Dependencies != null)
                        {
                            using (DataTable dependencies = new DataTable())
                            using (SqliteTransaction transaction = DBconnection.BeginTransaction())
                            {
                                dependencies.Columns.Add("Dependency", typeof(string));
                                foreach (string value in MainSaveFileInfoData.Dependencies.Where(x => x.RawDepType != "rdlc").Select(x => x.Raw.Value).Distinct())
                                    dependencies.Rows.Add(value);
                                using (SqliteCommand clear = DBconnection.CreateCommand())
                                { clear.Transaction = transaction; clear.CommandText = "DELETE FROM Dependencies"; clear.ExecuteNonQuery(); }
                                SqliteStorage.WriteRows(DBconnection, "Dependencies", dependencies, false, transaction);
                                transaction.Commit();
                            }
                        }
                        break;
                    case "CitysTable": InsertNames(target, "CityName", CitiesList.Select(x => x.CityName)); break;
                    case "CompaniesTable": InsertNames(target, "CompanyName", CompaniesList); break;
                    case "TrucksTable":
                        using (DataTable trucks = new DataTable())
                        {
                            trucks.Columns.Add("TruckName", typeof(string)); trucks.Columns.Add("TruckType", typeof(int));
                            foreach (CompanyTruck truck in CompanyTruckList) trucks.Rows.Add(truck.TruckName, truck.Type);
                            SqliteStorage.WriteRows(DBconnection, target, trucks, true);
                        }
                        break;
                    case "TrailerTables":
                        InsertNames("TrailerDefinitionTable", "TrailerDefinitionName", TrailerDefinitionVariants.Keys);
                        InsertNames("TrailerVariantTable", "TrailerVariantName", TrailerVariants);
                        SqliteStorage.Execute(DBconnection, "DELETE FROM tempBulkTrailerDefinitionVariants");
                        using (DataTable variants = new DataTable())
                        {
                            variants.Columns.Add("TrailerDefinitionName", typeof(string)); variants.Columns.Add("TrailerVariantName", typeof(string));
                            foreach (KeyValuePair<string, List<string>> definition in TrailerDefinitionVariants)
                                foreach (string variant in definition.Value.Distinct()) variants.Rows.Add(definition.Key, variant);
                            SqliteStorage.WriteRows(DBconnection, "tempBulkTrailerDefinitionVariants", variants);
                        }
                        SqliteStorage.Execute(DBconnection, "INSERT INTO TrailerDefinitionToTrailerVariantTable (TrailerDefinitionID,TrailerVariantID) " +
                            "SELECT DISTINCT d.ID_trailerD,v.ID_trailerV FROM tempBulkTrailerDefinitionVariants t " +
                            "JOIN TrailerDefinitionTable d ON d.TrailerDefinitionName=t.TrailerDefinitionName " +
                            "JOIN TrailerVariantTable v ON v.TrailerVariantName=t.TrailerVariantName WHERE 1 ON CONFLICT DO NOTHING");
                        SqliteStorage.Execute(DBconnection, "DELETE FROM tempBulkTrailerDefinitionVariants");
                        break;
                    case "CargoesTable":
                        InsertNames(target, "CargoName", CargoesList.Select(x => x.CargoName));
                        InsertNames("TrailerDefinitionTable", "TrailerDefinitionName", CargoesList.SelectMany(x => x.TrailerDefList).Select(x => x.DefName));
                        SqliteStorage.Execute(DBconnection, "DELETE FROM tempBulkCargoesToTrailerDefinitionTable");
                        using (DataTable cargoDefinitions = new DataTable())
                        {
                            cargoDefinitions.Columns.Add("CargoName", typeof(string)); cargoDefinitions.Columns.Add("TrailerDefinitionName", typeof(string)); cargoDefinitions.Columns.Add("CargoType", typeof(int));
                            foreach (Cargo cargo in CargoesList)
                                foreach (TrailerDefinition definition in cargo.TrailerDefList) cargoDefinitions.Rows.Add(cargo.CargoName, definition.DefName, definition.CargoType);
                            SqliteStorage.WriteRows(DBconnection, "tempBulkCargoesToTrailerDefinitionTable", cargoDefinitions);
                        }
                        SqliteStorage.Execute(DBconnection, "INSERT INTO CargoesToTrailerDefinitionTable (CargoID,TrailerDefinitionID,CargoType) " +
                            "SELECT DISTINCT c.ID_cargo,d.ID_trailerD,t.CargoType FROM tempBulkCargoesToTrailerDefinitionTable t " +
                            "JOIN CargoesTable c ON c.CargoName=t.CargoName JOIN TrailerDefinitionTable d ON d.TrailerDefinitionName=t.TrailerDefinitionName WHERE 1 ON CONFLICT DO NOTHING");
                        SqliteStorage.Execute(DBconnection, "DELETE FROM tempBulkCargoesToTrailerDefinitionTable");
                        break;
                    case "DistancesTable":
                        SqliteStorage.Execute(DBconnection, "DELETE FROM tempBulkDistancesTable");
                        using (DataTable distances = new DataTable())
                        {
                            foreach (string column in new[] { "SourceCity", "SourceCompany", "DestinationCity", "DestinationCompany" }) distances.Columns.Add(column, typeof(string));
                            foreach (string column in new[] { "Distance", "FerryTime", "FerryPrice" }) distances.Columns.Add(column, typeof(int));
                            foreach (string companyId in SiiNunitData.Economy.companies)
                                foreach (string offerId in ((Save.Items.Company)SiiNunitData.SiiNitems[companyId]).job_offer)
                                {
                                    Save.Items.Job_offer_Data offer = (Save.Items.Job_offer_Data)SiiNunitData.SiiNitems[offerId];
                                    if (string.IsNullOrEmpty(offer.target.Value)) continue;
                                    string[] source = companyId.Split('.'); string[] destination = offer.target.Value.Split('.');
                                    if (source.Length < 4 || destination.Length < 2) continue;
                                    distances.Rows.Add(source[3], source[2], destination[1], destination[0], offer.shortest_distance_km, offer.ferry_time, offer.ferry_price);
                                }
                            SqliteStorage.WriteRows(DBconnection, "tempBulkDistancesTable", distances);
                        }
                        // UPDATE returning zero is not an exception: use a real upsert for new routes.
                        SqliteStorage.Execute(DBconnection, "INSERT INTO DistancesTable (SourceCityID,SourceCompanyID,DestinationCityID,DestinationCompanyID,Distance,FerryTime,FerryPrice) " +
                            "SELECT sc.ID_city,sp.ID_company,dc.ID_city,dp.ID_company,t.Distance,t.FerryTime,t.FerryPrice FROM tempBulkDistancesTable t " +
                            "JOIN CitysTable sc ON sc.CityName=t.SourceCity JOIN CompaniesTable sp ON sp.CompanyName=t.SourceCompany " +
                            "JOIN CitysTable dc ON dc.CityName=t.DestinationCity JOIN CompaniesTable dp ON dp.CompanyName=t.DestinationCompany WHERE 1 " +
                            "ON CONFLICT(SourceCityID,SourceCompanyID,DestinationCityID,DestinationCompanyID) DO UPDATE SET Distance=excluded.Distance,FerryTime=excluded.FerryTime,FerryPrice=excluded.FerryPrice");
                        SqliteStorage.Execute(DBconnection, "DELETE FROM tempBulkDistancesTable");
                        break;
                }
            }
            finally { DBconnection.Close(); }
        }

        private void GetDataFromDatabase(string _targetTable)
        {
            DataTableReader reader = null;

            try
            {
                if (DBconnection.State == ConnectionState.Closed)
                    DBconnection.Open();

                int totalrecord = 0;

                switch (_targetTable)
                {
                    case "Dependencies":
                        {
                            DBDependencies.Clear();

                            string commandText = "SELECT Dependency FROM [Dependencies];";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                DBDependencies.Add(reader["Dependency"].ToString());
                            }

                            totalrecord = DBDependencies.Count();

                            break;
                        }

                    case "CargoesTable":
                        {
                            CargoesListDB.Clear();
                            
                            string commandText = "SELECT ID_cargo, CargoName FROM [CargoesTable];";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                List<TrailerDefinition> tempDefVars = new List<TrailerDefinition>();

                                commandText = "SELECT TrailerDefinitionID, CargoType FROM [CargoesToTrailerDefinitionTable] WHERE CargoID = '" + reader["ID_cargo"].ToString() + "';";

                                try
                                {
                                    DataTableReader reader2 = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                                    Dictionary<string, int> tempVar = new Dictionary<string, int>();

                                    while (reader2.Read())
                                    {
                                        commandText = "SELECT TrailerDefinitionName FROM [TrailerDefinitionTable] WHERE ID_trailerD = '" + reader2["TrailerDefinitionID"].ToString() + "';";
                                        
                                        DataTableReader reader3 = SqliteStorage.ReadSnapshot(DBconnection, commandText);
                                        while (reader3.Read())
                                        {
                                            tempDefVars.Add(new TrailerDefinition(reader3["TrailerDefinitionName"].ToString(), int.Parse(reader2["CargoType"].ToString()), "1"));
                                        }
                                    }
                                }
                                catch (SqliteException ex)
                                {
                                    string avsd = ex.Message;
                                }
                                catch (Exception ex)
                                {
                                    string avsd = ex.Message;
                                }

                                CargoesListDB.Add(new Cargo(reader["CargoName"].ToString(), tempDefVars));
                            }

                            totalrecord = CargoesListDB.Count();

                            break;
                        }

                    case "CitysTable":
                        {
                            CitiesListDB.Clear();

                            string commandText = "SELECT CityName FROM [CitysTable];";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                CitiesListDB.Add(reader["CityName"].ToString());
                            }

                            totalrecord = CitiesListDB.Count();

                            break;
                        }

                    case "CompaniesTable":
                        {
                            CompaniesListDB.Clear();

                            string commandText = "SELECT CompanyName FROM [CompaniesTable];";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                CompaniesListDB.Add(reader["CompanyName"].ToString());
                            }

                            totalrecord = CompaniesListDB.Count();

                            break;
                        }

                    case "TrucksTable":
                        {
                            CompanyTruckListDB.Clear();

                            string commandText = "SELECT TruckName, TruckType FROM [TrucksTable];";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                CompanyTruckListDB.Add(new CompanyTruck(reader["TruckName"].ToString(), int.Parse(reader["TruckType"].ToString())));
                            }

                            totalrecord = CompanyTruckListDB.Count();

                            break;
                        }

                    case "TrailerDefinition":
                        {
                            TrailerDefinitionListDB.Clear();

                            string commandText = "SELECT TrailerDefinitionName FROM [TrailerDefinitionTable];";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                TrailerDefinitionListDB.Add(reader["TrailerDefinitionName"].ToString());
                            }

                            totalrecord = TrailerDefinitionListDB.Count();

                            break;
                        }

                    case "TrailerVariants":
                        {
                            TrailerVariantsListDB.Clear();

                            string commandText = "SELECT TrailerVariantName FROM [TrailerVariantTable];";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                TrailerVariantsListDB.Add(reader["TrailerVariantName"].ToString());
                            }

                            totalrecord = TrailerVariantsListDB.Count();

                            break;
                        }

                    case "TrailerDefinitionVariants":
                        {
                            TrailerDefinitionVariantsDB.Clear();

                            string commandText = "SELECT TrailerDefinitionTable.TrailerDefinitionName, TrailerVariantTable.TrailerVariantName " +
                                "FROM TrailerDefinitionToTrailerVariantTable " +
                                "INNER JOIN TrailerDefinitionTable ON TrailerDefinitionToTrailerVariantTable.TrailerDefinitionID = TrailerDefinitionTable.ID_trailerD " +
                                "INNER JOIN TrailerVariantTable ON TrailerDefinitionToTrailerVariantTable.TrailerVariantID = TrailerVariantTable.ID_trailerV;";

                            reader = SqliteStorage.ReadSnapshot(DBconnection, commandText);

                            while (reader.Read())
                            {
                                string DefinitionName = reader["TrailerDefinitionName"].ToString();

                                if (!TrailerDefinitionVariantsDB.ContainsKey(DefinitionName))
                                {
                                    TrailerDefinitionVariantsDB.Add(DefinitionName, new List<string>());
                                }

                                TrailerDefinitionVariantsDB[DefinitionName].Add(reader["TrailerVariantName"].ToString());
                            }

                            totalrecord = TrailerDefinitionVariantsDB.Count();

                            break;
                        }
                }

                IO_Utilities.LogWriter("Loaded " + totalrecord + " entries from " + _targetTable + " table.");
            }
            catch
            {
                IO_Utilities.LogWriter("Missing " + DBconnection.DataSource + " file");
            }
            finally
            {
                if (reader != null)
                    reader.Close();

                DBconnection.Close();
            }

        }

        //External Data

        private void ExtDataCreateDatabase(string database)
        {
            SqliteStorage.Ensure(database, DatabaseKind.External);
        }

        private void ExtDataInsertDataIntoDatabase(string database, string target, object data)
        {
            using (SqliteConnection connection = SqliteStorage.OpenConnection(database))
            {
                connection.Open();
                using (SqliteTransaction transaction = connection.BeginTransaction())
                {
                    void Execute(string sql, params object[] values)
                    {
                        using (SqliteCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction; command.CommandText = sql;
                            for (int i = 0; i < values.Length; i++) command.Parameters.AddWithValue("$p" + i, values[i]);
                            command.ExecuteNonQuery();
                        }
                    }
                    long FindId(string table, string id, string column, string value)
                    {
                        using (SqliteCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = "SELECT " + SqliteStorage.Quote(id) + " FROM " + SqliteStorage.Quote(table) + " WHERE " + SqliteStorage.Quote(column) + "=$value";
                            command.Parameters.AddWithValue("$value", value);
                            return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                        }
                    }
                    if (target == "CargoesTable")
                    {
                        List<ExtCargo> cargos = (List<ExtCargo>)data;
                        Execute("DELETE FROM BodyTypesToCargoTable; DELETE FROM BodyTypesTable; DELETE FROM CargoesTable");
                        foreach (string body in cargos.SelectMany(x => x.BodyTypes).Distinct())
                            Execute("INSERT INTO BodyTypesTable (BodyTypeName) VALUES ($p0)", body);
                        foreach (ExtCargo cargo in cargos)
                        {
                            Execute("INSERT INTO CargoesTable (CargoName,ADRclass,Fragility,Mass,UnitRewardpPerKM,Valuable,Overweight) VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6)",
                                cargo.CargoName, cargo.ADRclass, cargo.Fragility, cargo.Mass, cargo.UnitRewardpPerKM, cargo.Valuable, cargo.Overweight);
                            long cargoId = FindId("CargoesTable", "ID_cargo", "CargoName", cargo.CargoName);
                            foreach (string body in cargo.BodyTypes.Distinct())
                                Execute("INSERT INTO BodyTypesToCargoTable (CargoID,BodyTypeID) VALUES ($p0,$p1)", cargoId, FindId("BodyTypesTable", "ID_bodytype", "BodyTypeName", body));
                        }
                    }
                    else if (target == "CompaniesTable")
                    {
                        List<ExtCompany> companies = (List<ExtCompany>)data;
                        Execute("DELETE FROM CompaniesCargoesInTable; DELETE FROM CompaniesCargoesOutTable; DELETE FROM CompaniesTable; DELETE FROM AllCargoesTable");
                        foreach (string cargo in companies.SelectMany(x => x.inCargo.Concat(x.outCargo)).Distinct())
                            Execute("INSERT INTO AllCargoesTable (CargoName) VALUES ($p0)", cargo);
                        foreach (ExtCompany company in companies)
                        {
                            Execute("INSERT INTO CompaniesTable (CompanyName) VALUES ($p0)", company.CompanyName);
                            long companyId = FindId("CompaniesTable", "ID_company", "CompanyName", company.CompanyName);
                            foreach (string cargo in company.inCargo.Distinct())
                                Execute("INSERT INTO CompaniesCargoesInTable (CompanyID,CargoID) VALUES ($p0,$p1)", companyId, FindId("AllCargoesTable", "ID_cargo", "CargoName", cargo));
                            foreach (string cargo in company.outCargo.Distinct())
                                Execute("INSERT INTO CompaniesCargoesOutTable (CompanyID,CargoID) VALUES ($p0,$p1)", companyId, FindId("AllCargoesTable", "ID_cargo", "CargoName", cargo));
                        }
                    }
                    else throw new ArgumentException("Unknown external table: " + target);
                    transaction.Commit();
                }
            }
        }

        private void LoadCachedExternalCargoData(string _dbname)
        {
            DataTableReader reader = null, reader2 = null;

            try
            {
                SqliteConnection tDBconnection;
                string _fileName = Directory.GetCurrentDirectory() + @"\gameref\cache\" + GameType + "\\" + _dbname + ".sqlite";

                if (!File.Exists(_fileName) && !File.Exists(Path.ChangeExtension(_fileName, ".sdf"))) return;
                SqliteStorage.Ensure(_fileName, DatabaseKind.External);
                tDBconnection = SqliteStorage.OpenConnection(_fileName);

                if (tDBconnection.State == ConnectionState.Closed)
                {
                    tDBconnection.Open();
                }

                string commandText = "SELECT ID_cargo, CargoName, ADRclass, Fragility, Mass, UnitRewardpPerKM, Valuable, Overweight FROM [CargoesTable]";

                reader = SqliteStorage.ReadSnapshot(tDBconnection, commandText);

                while (reader.Read())
                {
                    ExtCargo tempExtCargo = new ExtCargo(reader["CargoName"].ToString());

                    tempExtCargo.Fragility = Convert.ToDecimal(reader["Fragility"], CultureInfo.InvariantCulture);

                    tempExtCargo.ADRclass = int.Parse(reader["ADRclass"].ToString());

                    tempExtCargo.Mass = Convert.ToDecimal(reader["Mass"], CultureInfo.InvariantCulture);

                    tempExtCargo.UnitRewardpPerKM = Convert.ToDecimal(reader["UnitRewardpPerKM"], CultureInfo.InvariantCulture);

                    //tempExtCargo.Groups.Add(reader["CargoName"].ToString());

                    //tempExtCargo.MaxDistance = int.Parse(reader["CargoName"].ToString());

                    //tempExtCargo.Volume = decimal.Parse(reader["CargoName"].ToString());

                    tempExtCargo.Valuable = Convert.ToInt64(reader["Valuable"], CultureInfo.InvariantCulture) != 0;

                    tempExtCargo.Overweight = Convert.ToInt64(reader["Overweight"], CultureInfo.InvariantCulture) != 0;

                    commandText = "SELECT BodyTypesTable.BodyTypeName FROM [BodyTypesToCargoTable] INNER JOIN [BodyTypesTable] ON BodyTypesTable.ID_bodytype = BodyTypesToCargoTable.BodyTypeID WHERE BodyTypesToCargoTable.CargoID = '" + reader["ID_cargo"].ToString() + "';";

                    reader2 = SqliteStorage.ReadSnapshot(tDBconnection, commandText);

                    while (reader2.Read())
                    {
                        tempExtCargo.BodyTypes.Add(reader2["BodyTypeName"].ToString());
                    }

                    ExtCargoList.Add(tempExtCargo);
                }

                commandText = "SELECT ID_company, CompanyName FROM [CompaniesTable]";

                reader = SqliteStorage.ReadSnapshot(tDBconnection, commandText);

                while (reader.Read())
                {
                    int compindex = ExternalCompanies.FindIndex(x => x.CompanyName == reader["CompanyName"].ToString());

                    if (compindex == -1)
                    {
                        ExtCompany tempExtCompany = new ExtCompany(reader["CompanyName"].ToString());

                        commandText = "SELECT AllCargoesTable.CargoName FROM [CompaniesCargoesOutTable] INNER JOIN [AllCargoesTable] ON AllCargoesTable.ID_cargo = CompaniesCargoesOutTable.CargoID WHERE CompaniesCargoesOutTable.CompanyID = '" + reader["ID_company"].ToString() + "';";

                        reader2 = SqliteStorage.ReadSnapshot(tDBconnection, commandText);

                        while (reader2.Read())
                        {
                            tempExtCompany.outCargo.Add(reader2["CargoName"].ToString());
                        }

                        ExternalCompanies.Add(tempExtCompany);
                    }
                    else
                    {
                        commandText = "SELECT AllCargoesTable.CargoName FROM [CompaniesCargoesOutTable] INNER JOIN [AllCargoesTable] ON AllCargoesTable.ID_cargo = CompaniesCargoesOutTable.CargoID WHERE CompaniesCargoesOutTable.CompanyID = '" + reader["ID_company"].ToString() + "';";

                        reader2 = SqliteStorage.ReadSnapshot(tDBconnection, commandText);

                        while (reader2.Read())
                        {
                            ExternalCompanies[compindex].outCargo.Add(reader2["CargoName"].ToString());
                        }
                    }
                }

                tDBconnection.Close();
            }
            catch
            { }
        }
    }
}
