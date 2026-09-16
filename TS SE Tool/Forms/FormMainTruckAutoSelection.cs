/*
   Copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace TS_SE_Tool
{
    public partial class FormMain
    {
        private static readonly bool truckAutoSelectionHookInstalled = InstallTruckAutoSelectionHook();
        private object lastAutoSelectedTruckDataSource;

        private static bool InstallTruckAutoSelectionHook()
        {
            Application.Idle += CheckTruckAutoSelectionOnIdle;
            return true;
        }

        private static void CheckTruckAutoSelectionOnIdle(object sender, EventArgs e)
        {
            FormMain form = Application.OpenForms.OfType<FormMain>().FirstOrDefault();
            if (form == null || form.IsDisposed || !form.IsHandleCreated) return;
            form.ApplyOnlyTruckSelectionAfterBinding();
        }

        private void ApplyOnlyTruckSelectionAfterBinding()
        {
            object dataSource = comboBoxUserTruckCompanyTrucks.DataSource;
            if (dataSource == null || ReferenceEquals(dataSource, lastAutoSelectedTruckDataSource)) return;

            DataTable table = dataSource as DataTable;
            DataView view = dataSource as DataView;
            DataRowView[] rows;
            if (view != null)
                rows = view.Cast<DataRowView>().Where(IsRealTruckRow).ToArray();
            else if (table != null)
                rows = table.DefaultView.Cast<DataRowView>().Where(IsRealTruckRow).ToArray();
            else
                rows = comboBoxUserTruckCompanyTrucks.Items.Cast<object>().OfType<DataRowView>().Where(IsRealTruckRow).ToArray();

            // Mark this binding only after it has produced rows. This also supports
            // loading another profile, because that creates a new DataTable instance.
            if (rows.Length == 0) return;
            lastAutoSelectedTruckDataSource = dataSource;
            if (rows.Length != 1) return;

            object truckId = rows[0].Row["UserTruckNameless"];
            if (truckId == null || truckId == DBNull.Value) return;

            comboBoxUserTruckCompanyTrucks.Enabled = true;

            // Data binding often preselects row zero before the normal change handler
            // can initialize the truck panel. Force a genuine transition after binding.
            comboBoxUserTruckCompanyTrucks.SelectedIndex = -1;
            comboBoxUserTruckCompanyTrucks.SelectedValue = truckId;
            if (comboBoxUserTruckCompanyTrucks.SelectedIndex < 0)
                comboBoxUserTruckCompanyTrucks.SelectedIndex = 0;

            comboBoxCompanyTrucks_SelectedIndexChanged(comboBoxUserTruckCompanyTrucks, EventArgs.Empty);
        }

        private static bool IsRealTruckRow(DataRowView row)
        {
            if (row == null || !row.Row.Table.Columns.Contains("UserTruckNameless")) return false;
            object value = row.Row["UserTruckNameless"];
            return value != null && value != DBNull.Value && !string.IsNullOrWhiteSpace(value.ToString()) && !string.Equals(value.ToString(), "null", StringComparison.OrdinalIgnoreCase);
        }
    }
}
