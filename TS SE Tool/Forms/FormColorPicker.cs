/*
   Copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System.Drawing;
using System.Windows.Forms;

namespace OpenPainter.ColorPicker
{
    public sealed class FormColorPicker : Form
    {
        public Color PrimaryColor { get; private set; }
        private readonly Panel preview;

        public FormColorPicker(Color color)
        {
            PrimaryColor = color;
            Text = "Color"; StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false; MaximizeBox = false; ClientSize = new Size(400, 110);
            preview = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = color.A == 0 ? SystemColors.Control : color };
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            Button choose = new Button { Text = "Choose…", AutoSize = true };
            choose.Click += (sender, args) =>
            {
                using (ColorDialog picker = new ColorDialog { FullOpen = true, Color = PrimaryColor.A == 0 ? Color.White : PrimaryColor })
                    if (picker.ShowDialog(this) == DialogResult.OK) { PrimaryColor = picker.Color; preview.BackColor = picker.Color; }
            };
            Button clear = new Button { Text = "Clear", AutoSize = true };
            clear.Click += (sender, args) => { PrimaryColor = Color.Transparent; preview.BackColor = SystemColors.Control; };
            Button apply = new Button { Text = "OK", AutoSize = true, DialogResult = DialogResult.OK };
            Button cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
            buttons.Controls.AddRange(new Control[] { choose, clear, apply, cancel });
            Controls.Add(buttons); Controls.Add(preview);
            AcceptButton = apply; CancelButton = cancel;
        }
    }
}
