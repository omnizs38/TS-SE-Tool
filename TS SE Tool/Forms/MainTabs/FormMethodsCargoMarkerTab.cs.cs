/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TS_SE_Tool
{
    public partial class FormMain
    {
        private const string CargoSeedsHeader = "TSSE_CARGO_SEEDS_V1";
        private const int DefaultCargoSeedCount = 10;

        private void FillFormCargoOffersControls()
        {
            FillCargoMarketCities();
            FillTrailerTypesCM();
            PrintCargoSeeds();
        }

        private void FillCargoMarketCities()
        {
            DataTable table = new DataTable();
            DataColumn cityColumn = table.Columns.Add("City", typeof(string));
            table.PrimaryKey = new[] { cityColumn };
            table.Columns.Add("CityName", typeof(string));

            if (CitiesList != null)
            {
                foreach (City city in CitiesList.Where(item => item != null && !item.Disabled).OrderBy(item => item.CityNameTranslated))
                {
                    table.Rows.Add(city.CityName, string.IsNullOrWhiteSpace(city.CityNameTranslated) ? city.CityName : city.CityNameTranslated);
                }
            }

            comboBoxCargoMarketSourceCity.ValueMember = "City";
            comboBoxCargoMarketSourceCity.DisplayMember = "CityName";
            comboBoxCargoMarketSourceCity.DataSource = table;

            if (SiiNunitData != null && SiiNunitData.Economy != null &&
                table.Rows.Find(SiiNunitData.Economy.last_visited_city) != null)
            {
                comboBoxCargoMarketSourceCity.SelectedValue = SiiNunitData.Economy.last_visited_city;
            }
        }

        private void comboBoxSourceCityCM_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetupSourceCompaniesCM();
            if (comboBoxCargoMarketSourceCompany.Items.Count > 0 && comboBoxCargoMarketSourceCompany.SelectedIndex < 0)
            {
                comboBoxCargoMarketSourceCompany.SelectedIndex = 0;
            }
            PreparePossibleCargoes();
            PrintCargoSeeds();
        }

        private void SetupSourceCompaniesCM()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Company", typeof(string));
            table.Columns.Add("CompanyName", typeof(string));

            City city = GetSelectedCargoCity();
            if (city != null)
            {
                foreach (Company company in city.ReturnCompanies()
                    .Where(item => item != null && !item.Excluded)
                    .OrderBy(item => item.CompanyNameTranslated))
                {
                    string displayName = string.IsNullOrWhiteSpace(company.CompanyNameTranslated)
                        ? company.CompanyName
                        : company.CompanyNameTranslated;
                    table.Rows.Add(company.CompanyName, displayName);
                }
            }

            comboBoxCargoMarketSourceCompany.ValueMember = "Company";
            comboBoxCargoMarketSourceCompany.DisplayMember = "CompanyName";
            comboBoxCargoMarketSourceCompany.DataSource = table;
        }

        private void comboBoxSourceCompanyCM_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreparePossibleCargoes();
            PrintCargoSeeds();
        }

        private void comboBoxCMTrailerTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreparePossibleCargoes();
        }

        private void PreparePossibleCargoes()
        {
            listBoxCargoMarketCargoListForCompany.BeginUpdate();
            try
            {
                listBoxCargoMarketCargoListForCompany.Items.Clear();
                Company company = GetSelectedCargoCompany();
                if (company == null)
                {
                    return;
                }

                IEnumerable<string> cargoNames = Enumerable.Empty<string>();
                if (ExternalCompanies != null)
                {
                    ExtCompany externalCompany = ExternalCompanies.Find(item =>
                        item != null && string.Equals(item.CompanyName, company.CompanyName, StringComparison.OrdinalIgnoreCase));
                    if (externalCompany != null && externalCompany.outCargo != null)
                    {
                        cargoNames = externalCompany.outCargo;
                    }
                }

                if (!cargoNames.Any() && CargoesList != null)
                {
                    cargoNames = CargoesList.Where(item => item != null).Select(item => item.CargoName);
                }

                string selectedTrailerType = comboBoxCMTrailerTypes.SelectedValue == null
                    ? string.Empty
                    : comboBoxCMTrailerTypes.SelectedValue.ToString();

                foreach (string cargoName in cargoNames.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().OrderBy(name => name))
                {
                    ExtCargo externalCargo = ExtCargoList == null
                        ? null
                        : ExtCargoList.Find(item => item != null && string.Equals(item.CargoName, cargoName, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(selectedTrailerType) && externalCargo != null &&
                        externalCargo.BodyTypes != null && !externalCargo.BodyTypes.Contains(selectedTrailerType))
                    {
                        continue;
                    }

                    string translated;
                    string display = CargoLngDict != null && CargoLngDict.TryGetValue(cargoName, out translated) &&
                        !string.IsNullOrWhiteSpace(translated)
                        ? translated + " [" + cargoName + "]"
                        : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cargoName.Replace('_', ' '));
                    listBoxCargoMarketCargoListForCompany.Items.Add(display);
                }
            }
            finally
            {
                listBoxCargoMarketCargoListForCompany.EndUpdate();
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            PrintCargoSeeds();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            PrintCargoSeeds();
        }

        private void PrintCargoSeeds()
        {
            listBoxCargoMarketSourceCargoSeeds.BeginUpdate();
            try
            {
                listBoxCargoMarketSourceCargoSeeds.Items.Clear();
                Company company = GetSelectedCargoCompany();
                if (company == null)
                {
                    SetCargoMarketStatus("Select a city and company to manage cargo offer seeds.", false);
                    return;
                }

                uint gameTime = SiiNunitData != null && SiiNunitData.Economy != null
                    ? (uint)Math.Max(0, SiiNunitData.Economy.game_time)
                    : 0;

                uint[] seeds = company.CargoSeeds ?? new uint[0];
                foreach (uint seed in seeds.OrderBy(value => value))
                {
                    long minutes = (long)seed - gameTime;
                    string remaining = minutes >= 0
                        ? string.Format(CultureInfo.CurrentCulture, "{0} d {1:00} h {2:00} m", minutes / 1440, (minutes / 60) % 24, minutes % 60)
                        : "expired " + Math.Abs(minutes).ToString(CultureInfo.CurrentCulture) + " min ago";
                    listBoxCargoMarketSourceCargoSeeds.Items.Add(seed.ToString(CultureInfo.InvariantCulture).PadRight(12) + " | " + remaining);
                }

                SetCargoMarketStatus(
                    seeds.Length == 0
                        ? "No cargo offers are stored for the selected company. Generate or paste a list."
                        : seeds.Length + " cargo offer seeds are ready. Write the save to apply the changes.",
                    false);
            }
            finally
            {
                listBoxCargoMarketSourceCargoSeeds.EndUpdate();
            }
        }

        private void buttonCargoMarketRandomizeCargoCompany_Click(object sender, EventArgs e)
        {
            Company company = GetSelectedCargoCompany();
            if (company == null)
            {
                SetCargoMarketStatus("Select a company first.", true);
                return;
            }

            company.CargoSeeds = CreateCargoSeeds(DefaultCargoSeedCount);
            PrintCargoSeeds();
        }

        private void buttonCargoMarketResetCargoCompany_Click(object sender, EventArgs e)
        {
            Company company = GetSelectedCargoCompany();
            if (company == null)
            {
                SetCargoMarketStatus("Select a company first.", true);
                return;
            }

            company.CargoSeeds = new uint[0];
            PrintCargoSeeds();
        }

        private void buttonCargoMarketRandomizeCargoCity_Click(object sender, EventArgs e)
        {
            City city = GetSelectedCargoCity();
            if (city == null)
            {
                SetCargoMarketStatus("Select a city first.", true);
                return;
            }

            foreach (Company company in city.ReturnCompanies().Where(item => item != null && !item.Excluded))
            {
                company.CargoSeeds = CreateCargoSeeds(DefaultCargoSeedCount);
            }
            PrintCargoSeeds();
            SetCargoMarketStatus("Cargo offers were generated for every company in the selected city.", false);
        }

        private void buttonCargoMarketResetCargoCity_Click(object sender, EventArgs e)
        {
            City city = GetSelectedCargoCity();
            if (city == null)
            {
                SetCargoMarketStatus("Select a city first.", true);
                return;
            }

            foreach (Company company in city.ReturnCompanies().Where(item => item != null && !item.Excluded))
            {
                company.CargoSeeds = new uint[0];
            }
            PrintCargoSeeds();
            SetCargoMarketStatus("Cargo offers were cleared for every company in the selected city.", false);
        }

        private void FillTrailerTypesCM()
        {
            DataTable table = new DataTable();
            table.Columns.Add("TrailerType", typeof(string));
            table.Columns.Add("TrailerTypeName", typeof(string));
            table.Rows.Add(string.Empty, "All trailer types");

            IEnumerable<string> trailerTypes = ExtCargoList == null
                ? Enumerable.Empty<string>()
                : ExtCargoList.Where(item => item != null && item.BodyTypes != null)
                    .SelectMany(item => item.BodyTypes)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct()
                    .OrderBy(item => item);

            foreach (string trailerType in trailerTypes)
            {
                table.Rows.Add(trailerType, CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trailerType.Replace('_', ' ')));
            }

            comboBoxCMTrailerTypes.ValueMember = "TrailerType";
            comboBoxCMTrailerTypes.DisplayMember = "TrailerTypeName";
            comboBoxCMTrailerTypes.DataSource = table;
        }

        private void buttonCargoMarketCopySeeds_Click(object sender, EventArgs e)
        {
            Company company = GetSelectedCargoCompany();
            City city = GetSelectedCargoCity();
            if (company == null || city == null)
            {
                SetCargoMarketStatus("Select a city and company first.", true);
                return;
            }

            StringBuilder payload = new StringBuilder();
            payload.AppendLine(CargoSeedsHeader);
            payload.AppendLine("city=" + city.CityName);
            payload.AppendLine("company=" + company.CompanyName);
            payload.AppendLine("seeds=" + string.Join(",", (company.CargoSeeds ?? new uint[0]).Select(value => value.ToString(CultureInfo.InvariantCulture))));
            Clipboard.SetText(payload.ToString());
            SetCargoMarketStatus("The selected company's cargo seeds were copied.", false);
        }

        private void buttonCargoMarketPasteSeeds_Click(object sender, EventArgs e)
        {
            Company company = GetSelectedCargoCompany();
            City city = GetSelectedCargoCity();
            if (company == null || city == null || !Clipboard.ContainsText())
            {
                SetCargoMarketStatus("Select a company and copy a valid seed package first.", true);
                return;
            }

            try
            {
                string[] lines = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                if (lines.Length < 4 || !string.Equals(lines[0].Trim(), CargoSeedsHeader, StringComparison.Ordinal))
                {
                    throw new FormatException("The clipboard does not contain a TS SE Tool cargo seed package.");
                }

                Dictionary<string, string> values = lines.Skip(1)
                    .Where(line => line.Contains("="))
                    .Select(line => line.Split(new[] { '=' }, 2))
                    .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);

                string sourceCity;
                string sourceCompany;
                values.TryGetValue("city", out sourceCity);
                values.TryGetValue("company", out sourceCompany);
                if ((!string.Equals(sourceCity, city.CityName, StringComparison.OrdinalIgnoreCase) ||
                     !string.Equals(sourceCompany, company.CompanyName, StringComparison.OrdinalIgnoreCase)) &&
                    MessageBox.Show("This package was copied from " + sourceCity + "/" + sourceCompany +
                        ". Apply it to the selected company anyway?", "Cargo seed package",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                string seedText;
                values.TryGetValue("seeds", out seedText);
                List<uint> seeds = new List<uint>();
                foreach (string value in (seedText ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    uint seed;
                    if (!uint.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out seed))
                    {
                        throw new FormatException("Invalid cargo seed: " + value);
                    }
                    seeds.Add(seed);
                }

                company.CargoSeeds = seeds.Distinct().OrderBy(value => value).Take(100).ToArray();
                PrintCargoSeeds();
                SetCargoMarketStatus("Cargo seeds were imported. Write the save to apply them.", false);
            }
            catch (Exception exception)
            {
                SetCargoMarketStatus(exception.Message, true);
            }
        }

        private uint[] CreateCargoSeeds(int count)
        {
            uint gameTime = SiiNunitData != null && SiiNunitData.Economy != null
                ? (uint)Math.Max(0, SiiNunitData.Economy.game_time)
                : 0;
            HashSet<uint> seeds = new HashSet<uint>();
            while (seeds.Count < count)
            {
                long candidate = (long)gameTime + RandomValue.Next(180, 1801);
                seeds.Add((uint)Math.Min(uint.MaxValue, candidate));
            }
            return seeds.OrderBy(value => value).ToArray();
        }

        private City GetSelectedCargoCity()
        {
            if (CitiesList == null || comboBoxCargoMarketSourceCity.SelectedValue == null)
            {
                return null;
            }
            string name = comboBoxCargoMarketSourceCity.SelectedValue.ToString();
            return CitiesList.Find(item => item != null && string.Equals(item.CityName, name, StringComparison.OrdinalIgnoreCase));
        }

        private Company GetSelectedCargoCompany()
        {
            City city = GetSelectedCargoCity();
            if (city == null || comboBoxCargoMarketSourceCompany.SelectedValue == null)
            {
                return null;
            }
            string name = comboBoxCargoMarketSourceCompany.SelectedValue.ToString();
            return city.ReturnCompanies().Find(item => item != null && string.Equals(item.CompanyName, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
