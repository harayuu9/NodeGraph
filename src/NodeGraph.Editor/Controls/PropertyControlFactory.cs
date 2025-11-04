using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using NodeGraph.Editor.ViewModels;
using NodeGraph.Model;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// プロパティの型と属性に基づいてUIコントロールを生成するファクトリクラスです。
/// </summary>
public static class PropertyControlFactory
{
    /// <summary>
    /// プロパティViewModelに対応するUIコントロールを生成します。
    /// </summary>
    /// <param name="propertyViewModel">プロパティViewModel</param>
    /// <returns>生成されたコントロール</returns>
    public static Control CreateControl(PropertyViewModel propertyViewModel)
    {
        var descriptor = propertyViewModel.Descriptor;
        var type = descriptor.Type;

        // Range属性がある数値型の場合はSliderを使用
        var rangeAttr = descriptor.GetAttribute<RangeAttribute>();
        if (rangeAttr != null && IsNumericType(type))
        {
            return CreateSlider(propertyViewModel, rangeAttr);
        }

        // Multiline属性がある文字列型の場合はTextBoxを複数行モードで使用
        var multilineAttr = descriptor.GetAttribute<MultilineAttribute>();
        if (multilineAttr != null && type == typeof(string))
        {
            return CreateMultilineTextBox(propertyViewModel, multilineAttr);
        }

        // 型に基づいてコントロールを生成
        if (type == typeof(float) || type == typeof(double) || type == typeof(int) || type == typeof(long) ||
            type == typeof(short) || type == typeof(byte) || type == typeof(decimal))
        {
            return CreateNumericUpDown(propertyViewModel);
        }
        else if (type == typeof(string))
        {
            return CreateTextBox(propertyViewModel);
        }
        else if (type == typeof(bool))
        {
            return CreateCheckBox(propertyViewModel);
        }
        else if (type.IsEnum)
        {
            return CreateComboBox(propertyViewModel);
        }

        // デフォルトはTextBox
        return CreateTextBox(propertyViewModel);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(float) || type == typeof(double) || type == typeof(int) ||
               type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
               type == typeof(decimal);
    }

    private static Control CreateSlider(PropertyViewModel propertyViewModel, RangeAttribute rangeAttr)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };

        var slider = new Slider
        {
            Minimum = rangeAttr.Min,
            Maximum = rangeAttr.Max,
            Width = 100,
            IsEnabled = !propertyViewModel.IsReadOnly
        };

        slider.Bind(RangeBase.ValueProperty, new Binding("Value")
        {
            Source = propertyViewModel,
            Mode = BindingMode.TwoWay,
        });
        panel.Children.Add(slider);

        var textBox = new TextBox
        {
            Width = 50,
            Height = 20,
            FontSize = 11,
            IsReadOnly = propertyViewModel.IsReadOnly
        };

        textBox.Bind(TextBox.TextProperty, new Binding("Value")
        {
            Source = propertyViewModel,
            Mode = BindingMode.TwoWay
        });
        panel.Children.Add(textBox);

        return panel;
    }

    private static Control CreateNumericUpDown(PropertyViewModel propertyViewModel)
    {
        var numericUpDown = new NumericUpDown
        {
            Width = 150,
            IsReadOnly = propertyViewModel.IsReadOnly
        };

        // 型に応じて適切な設定を行う
        var type = propertyViewModel.PropertyType;
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
        {
            numericUpDown.FormatString = "F0";
            numericUpDown.Increment = 1m;
        }
        else
        {
            numericUpDown.FormatString = "F2";
            numericUpDown.Increment = 0.1m;
        }

        numericUpDown.Bind(NumericUpDown.ValueProperty, new Binding("Value")
        {
            Source = propertyViewModel,
            Mode = BindingMode.TwoWay
        });

        return numericUpDown;
    }

    private static Control CreateTextBox(PropertyViewModel propertyViewModel)
    {
        var textBox = new TextBox
        {
            Width = 150,
            IsReadOnly = propertyViewModel.IsReadOnly
        };

        textBox.Bind(TextBox.TextProperty, new Binding("Value")
        {
            Source = propertyViewModel,
            Mode = BindingMode.TwoWay
        });

        if (!string.IsNullOrEmpty(propertyViewModel.Tooltip))
        {
            ToolTip.SetTip(textBox, propertyViewModel.Tooltip);
        }

        return textBox;
    }

    private static Control CreateMultilineTextBox(PropertyViewModel propertyViewModel, MultilineAttribute multilineAttr)
    {
        var textBox = new TextBox
        {
            Width = 150,
            Height = multilineAttr.Lines * 20,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            IsReadOnly = propertyViewModel.IsReadOnly
        };

        textBox.Bind(TextBox.TextProperty, new Binding("Value")
        {
            Source = propertyViewModel,
            Mode = BindingMode.TwoWay
        });

        if (!string.IsNullOrEmpty(propertyViewModel.Tooltip))
        {
            ToolTip.SetTip(textBox, propertyViewModel.Tooltip);
        }

        return textBox;
    }

    private static Control CreateCheckBox(PropertyViewModel propertyViewModel)
    {
        var checkBox = new CheckBox
        {
            Content = propertyViewModel.DisplayName,
            IsEnabled = !propertyViewModel.IsReadOnly
        };

        checkBox.Bind(CheckBox.IsCheckedProperty, new Binding("Value")
        {
            Source = propertyViewModel,
            Mode = BindingMode.TwoWay
        });

        if (!string.IsNullOrEmpty(propertyViewModel.Tooltip))
        {
            ToolTip.SetTip(checkBox, propertyViewModel.Tooltip);
        }

        return checkBox;
    }

    private static Control CreateComboBox(PropertyViewModel propertyViewModel)
    {
        var comboBox = new ComboBox
        {
            Width = 150,
            IsEnabled = !propertyViewModel.IsReadOnly
        };

        // Enum値をItemsSourceに設定
        var enumValues = Enum.GetValues(propertyViewModel.PropertyType);
        comboBox.ItemsSource = enumValues;

        comboBox.Bind(ComboBox.SelectedItemProperty, new Binding("Value")
        {
            Source = propertyViewModel,
            Mode = BindingMode.TwoWay
        });

        if (!string.IsNullOrEmpty(propertyViewModel.Tooltip))
        {
            ToolTip.SetTip(comboBox, propertyViewModel.Tooltip);
        }

        return comboBox;
    }
}
