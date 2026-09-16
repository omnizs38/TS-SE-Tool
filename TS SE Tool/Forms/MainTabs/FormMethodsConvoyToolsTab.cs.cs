/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TS_SE_Tool.Save.DataFormat;

namespace TS_SE_Tool
{
    public partial class FormMain
    {
        private const string PositionHeader = "TSSE_CONVOY_POSITION_V2";
        private const string RouteHeader = "TSSE_CONVOY_ROUTE_V2";
        private const string PackageHeader = "TSSE_CONVOY_PACKAGE_V2";

        private void buttonGPSCurrentPositionCopy_Click(object sender, EventArgs e)
        {
            if (!HasLoadedConvoyData())
            {
                SetConvoyStatus("Load a save before copying a truck position.", true);
                return;
            }

            SetClipboardPayload(PositionHeader + "\r\n" + SiiNunitData.Player.my_truck_placement);
            SetConvoyStatus("Truck position copied in the versioned convoy format.", false);
        }

        private void buttonGPSCurrentPositionPaste_Click(object sender, EventArgs e)
        {
            try
            {
                string payload = GetClipboardPayload();
                string placement;
                if (!TryReadPosition(payload, out placement))
                {
                    throw new FormatException("The clipboard does not contain a supported truck position package.");
                }

                SiiNunitData.Player.my_truck_placement = new SCS_Placement(placement);
                SetConvoyStatus("Truck position imported. Write the save after verifying it in game.", false);
            }
            catch (Exception exception)
            {
                SetConvoyStatus("Could not import truck position: " + exception.Message, true);
            }
        }

        private void buttonGPSStoredGPSPathCopy_Click(object sender, EventArgs e)
        {
            if (!HasLoadedConvoyData())
            {
                SetConvoyStatus("Load a save before copying a GPS route.", true);
                return;
            }

            SetClipboardPayload(BuildRoutePayload());
            SetConvoyStatus("Complete GPS route copied in the versioned convoy format.", false);
        }

        private void buttonGPSStoredGPSPathPaste_Click(object sender, EventArgs e)
        {
            try
            {
                string payload = GetClipboardPayload();
                Dictionary<string, List<List<string>>> route;
                if (!TryReadRoute(payload, out route))
                {
                    throw new FormatException("The clipboard does not contain a supported GPS route package.");
                }

                ApplyRoute(route);
                SetConvoyStatus("GPS route imported. Write the save after reviewing the route.", false);
            }
            catch (Exception exception)
            {
                SetConvoyStatus("Could not import GPS route: " + exception.Message, true);
            }
        }

        private void buttonConvoyToolsGPSTruckPositionMultySaveCopy_Click(object sender, EventArgs e)
        {
            using (FormConvoyControlPositions window = new FormConvoyControlPositions(true))
            {
                window.ShowDialog(this);
            }
        }

        private void buttonConvoyToolsGPSTruckPositionMultySavePaste_Click(object sender, EventArgs e)
        {
            using (FormConvoyControlPositions window = new FormConvoyControlPositions(false))
            {
                window.ShowDialog(this);
            }
        }

        private void buttonConvoyExportPackage_Click(object sender, EventArgs e)
        {
            if (!HasLoadedConvoyData())
            {
                SetConvoyStatus("Load a save before exporting convoy data.", true);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "TS SE Tool convoy package (*.tsconvoy)|*.tsconvoy|Text files (*.txt)|*.txt";
                dialog.DefaultExt = "tsconvoy";
                dialog.AddExtension = true;
                dialog.FileName = "convoy-package.tsconvoy";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, BuildFullPackage(), new UTF8Encoding(false));
                SetConvoyStatus("Convoy package exported to " + Path.GetFileName(dialog.FileName) + ".", false);
            }
        }

        private void buttonConvoyImportPackage_Click(object sender, EventArgs e)
        {
            if (!HasLoadedConvoyData())
            {
                SetConvoyStatus("Load a save before importing convoy data.", true);
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "TS SE Tool convoy package (*.tsconvoy)|*.tsconvoy|Text files (*.txt)|*.txt";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    string payload = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                    string placement;
                    Dictionary<string, List<List<string>>> route;
                    if (!TryReadFullPackage(payload, out placement, out route))
                    {
                        throw new FormatException("Unsupported or damaged convoy package.");
                    }

                    SiiNunitData.Player.my_truck_placement = new SCS_Placement(placement);
                    ApplyRoute(route);
                    SetConvoyStatus("Position and GPS route imported. Write the save after verification.", false);
                }
                catch (Exception exception)
                {
                    SetConvoyStatus("Could not import convoy package: " + exception.Message, true);
                }
            }
        }

        private void buttonConvoyClearRoute_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Remove all stored GPS waypoints from the loaded save?", "Clear GPS route",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            EnsureRouteDictionaries();
            GPSbehind.Clear();
            GPSahead.Clear();
            GPSAvoid.Clear();
            SetConvoyStatus("Stored GPS route cleared. Write the save to apply the change.", false);
        }

        private bool HasLoadedConvoyData()
        {
            return SiiNunitData != null && SiiNunitData.Player != null;
        }

        private void EnsureRouteDictionaries()
        {
            if (GPSbehind == null) GPSbehind = new Dictionary<string, List<string>>();
            if (GPSahead == null) GPSahead = new Dictionary<string, List<string>>();
            if (GPSAvoid == null) GPSAvoid = new Dictionary<string, List<string>>();
        }

        private string BuildRoutePayload()
        {
            EnsureRouteDictionaries();
            StringBuilder output = new StringBuilder();
            output.AppendLine(RouteHeader);
            AppendRouteSection(output, "BEHIND", GPSbehind);
            AppendRouteSection(output, "AHEAD", GPSahead);
            AppendRouteSection(output, "AVOID", GPSAvoid);
            return output.ToString();
        }

        private string BuildFullPackage()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(PackageHeader);
            output.AppendLine("[POSITION]");
            output.AppendLine(SiiNunitData.Player.my_truck_placement.ToString());
            string[] routeLines = NormalizeLines(BuildRoutePayload());
            for (int index = 1; index < routeLines.Length; index++)
            {
                if (!string.IsNullOrEmpty(routeLines[index])) output.AppendLine(routeLines[index]);
            }
            return output.ToString();
        }

        private static void AppendRouteSection(StringBuilder output, string name, Dictionary<string, List<string>> route)
        {
            output.AppendLine("[" + name + "]");
            foreach (KeyValuePair<string, List<string>> waypoint in route)
            {
                output.AppendLine("[WAYPOINT]");
                if (waypoint.Value == null) continue;
                foreach (string line in waypoint.Value)
                {
                    output.AppendLine(line ?? string.Empty);
                }
            }
        }

        private static bool TryReadPosition(string payload, out string placement)
        {
            placement = null;
            string[] lines = NormalizeLines(payload);
            if (lines.Length < 2) return false;

            if (string.Equals(lines[0], PositionHeader, StringComparison.Ordinal) ||
                string.Equals(lines[0], "GPS_TruckPosition", StringComparison.Ordinal))
            {
                placement = lines.Skip(1).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            }
            return !string.IsNullOrWhiteSpace(placement);
        }

        private bool TryReadRoute(string payload, out Dictionary<string, List<List<string>>> route)
        {
            string[] lines = NormalizeLines(payload);
            route = CreateEmptyRoute();
            if (lines.Length == 0 ||
                (!string.Equals(lines[0], RouteHeader, StringComparison.Ordinal) &&
                 !string.Equals(lines[0], "GPS_Path", StringComparison.Ordinal)))
            {
                return false;
            }

            string section = null;
            List<string> waypoint = null;
            for (int index = 1; index < lines.Length; index++)
            {
                string line = lines[index];
                string mappedSection = MapRouteSection(line);
                if (mappedSection != null)
                {
                    section = mappedSection;
                    waypoint = null;
                    continue;
                }

                if (string.Equals(line, "[WAYPOINT]", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(line, "waypoint", StringComparison.OrdinalIgnoreCase))
                {
                    if (section == null) continue;
                    waypoint = new List<string>();
                    route[section].Add(waypoint);
                    continue;
                }

                if (waypoint != null && !string.IsNullOrWhiteSpace(line))
                {
                    waypoint.Add(line);
                }
            }
            return true;
        }

        private bool TryReadFullPackage(string payload, out string placement, out Dictionary<string, List<List<string>>> route)
        {
            placement = null;
            route = null;
            string[] lines = NormalizeLines(payload);
            if (lines.Length < 3 || !string.Equals(lines[0], PackageHeader, StringComparison.Ordinal))
            {
                return false;
            }

            int positionMarker = Array.FindIndex(lines, line => string.Equals(line, "[POSITION]", StringComparison.Ordinal));
            if (positionMarker < 0 || positionMarker + 1 >= lines.Length)
            {
                return false;
            }
            placement = lines[positionMarker + 1];

            StringBuilder routePayload = new StringBuilder();
            routePayload.AppendLine(RouteHeader);
            foreach (string line in lines.Skip(positionMarker + 2))
            {
                routePayload.AppendLine(line);
            }
            return !string.IsNullOrWhiteSpace(placement) && TryReadRoute(routePayload.ToString(), out route);
        }

        private void ApplyRoute(Dictionary<string, List<List<string>>> route)
        {
            EnsureRouteDictionaries();
            ApplyRouteSection(GPSbehind, route["BEHIND"]);
            ApplyRouteSection(GPSahead, route["AHEAD"]);
            ApplyRouteSection(GPSAvoid, route["AVOID"]);
        }

        private void ApplyRouteSection(Dictionary<string, List<string>> target, IEnumerable<List<string>> source)
        {
            target.Clear();
            foreach (List<string> waypoint in source)
            {
                if (waypoint != null && waypoint.Count > 0)
                {
                    target.Add(GetSpareNameless(), new List<string>(waypoint));
                }
            }
        }

        private static Dictionary<string, List<List<string>>> CreateEmptyRoute()
        {
            return new Dictionary<string, List<List<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                { "BEHIND", new List<List<string>>() },
                { "AHEAD", new List<List<string>>() },
                { "AVOID", new List<List<string>>() }
            };
        }

        private static string MapRouteSection(string line)
        {
            if (string.Equals(line, "[BEHIND]", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(line, "GPSbehind", StringComparison.OrdinalIgnoreCase)) return "BEHIND";
            if (string.Equals(line, "[AHEAD]", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(line, "GPSahead", StringComparison.OrdinalIgnoreCase)) return "AHEAD";
            if (string.Equals(line, "[AVOID]", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(line, "GPSavoid", StringComparison.OrdinalIgnoreCase)) return "AVOID";
            return null;
        }

        private static string[] NormalizeLines(string payload)
        {
            return (payload ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static void SetClipboardPayload(string payload)
        {
            string encoded = BitConverter.ToString(Utilities.ZipDataUtilities.zipText(payload)).Replace("-", string.Empty);
            Clipboard.SetText(encoded);
        }

        private static string GetClipboardPayload()
        {
            if (!Clipboard.ContainsText()) throw new FormatException("The clipboard is empty.");
            return Utilities.ZipDataUtilities.unzipText(Clipboard.GetText().Trim());
        }
    }
}
