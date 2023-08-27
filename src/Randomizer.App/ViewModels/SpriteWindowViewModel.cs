﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Randomizer.Data.Options;

namespace Randomizer.App.ViewModels;

public class SpriteWindowViewModel : INotifyPropertyChanged
{
    private ObservableCollection<SpriteViewModel> _sprites = new();

    public ObservableCollection<SpriteViewModel> Sprites
    {
        get => _sprites;
        set => SetField(ref _sprites, value);
    }

    public SpriteFilter SpriteFilter { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
