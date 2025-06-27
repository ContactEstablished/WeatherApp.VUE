# 🌤️ Vue.js Weather Forecast App

A modern, responsive weather application built with Vue.js 3, Vuetify, and TailwindCSS. Get 3-day forecasts and 7-day weather history with a clean Material Design interface.

![Weather App Screenshot](https://via.placeholder.com/800x600/667eea/ffffff?text=Weather+App+Screenshot)

## ✨ Features

- **📍 Location Management**: Search by zip code and city, save up to 3 favorite locations
- **🌦️ 3-Day Forecast**: Detailed weather predictions with prominent temperature display
- **📅 7-Day History**: Historical weather data for the past week
- **💾 Persistent Storage**: Saved locations stored in browser cookies
- **📱 Responsive Design**: Works seamlessly on desktop, tablet, and mobile devices
- **🎨 Modern UI**: Clean Material Design interface with glass morphism effects
- **⚡ Fast Performance**: Lightweight and optimized for quick loading

## 🛠️ Tech Stack

- **Frontend Framework**: Vue.js 3
- **UI Components**: Vuetify 3.4.4
- **Styling**: TailwindCSS 2.2.19
- **Icons**: Font Awesome 6.4.0 & Material Design Icons
- **Storage**: Browser Cookies
- **Fonts**: Inter (Google Fonts)

## 🚀 Quick Start

### Prerequisites

- Modern web browser with JavaScript enabled
- Internet connection for CDN resources

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/ContactEstablished/WeatherApp.VUE.git
   cd vue-weather-app
   ```

2. **Open the application**
   ```bash
   # Simply open index.html in your browser
   open index.html
   # or
   # Use a local server (recommended)
   python -m http.server 8000
   # Then visit http://localhost:8000
   ```

3. **Start using the app**
   - Enter a zip code
   - Select a city from the dropdown
   - Click "Get Weather" to view forecast and history
   - Save locations for quick access

## 📱 Usage

### Getting Weather Data
1. Enter your zip code in the first field
2. Select your city from the dropdown menu
3. Click the "Get Weather" button
4. View your 3-day forecast and 7-day history

### Managing Locations
- **Save Location**: After searching for weather, click "Save Location" to add it to your favorites
- **Load Saved Location**: Click on any saved location card to quickly load its weather
- **Remove Location**: Click the red delete button on any saved location
- **Location Limit**: Maximum of 3 saved locations

### Weather Information Displayed
- **Temperature**: Large, prominent display
- **Weather Condition**: Clear description with emoji icons
- **Humidity**: Percentage humidity levels
- **Wind Speed**: Wind speed in mph
- **Feels Like**: Perceived temperature
- **High/Low**: Daily temperature range (history only)

## 🎨 Design Features

### Visual Elements
- **Glass Morphism**: Semi-transparent cards with backdrop blur
- **Gradient Background**: Beautiful purple-blue gradient
- **Hover Effects**: Smooth animations on interactive elements
- **Responsive Grid**: Adapts to different screen sizes
- **Material Design**: Clean, modern interface components

### Typography
- **Temperature Display**: Extra large, bold font (4rem, weight 900)
- **Inter Font**: Professional, readable typography
- **Hierarchical Text**: Clear information hierarchy
- **Icon Integration**: Weather and UI icons throughout

## 🔧 Customization

### Adding Real Weather API

Replace the mock data generation with real API calls:

```javascript
// Replace generateMockWeatherData() method
async generateRealWeatherData() {
    const apiKey = 'YOUR_API_KEY';
    const response = await fetch(
        `https://api.openweathermap.org/data/2.5/forecast?zip=${this.zipCode}&appid=${apiKey}`
    );
    const data = await response.json();
    // Process and return formatted data
}
```

### Customizing Colors

Update the Vuetify theme configuration:

```javascript
const vuetify = createVuetify({
    theme: {
        themes: {
            light: {
                colors: {
                    primary: '#your-color',
                    secondary: '#your-color',
                    // ... other colors
                }
            }
        }
    }
});
```

### Adding More Cities

Extend the `availableCities` array:

```javascript
availableCities: [
    'New York', 'Los Angeles', 'Chicago',
    // Add more cities here
    'Your City Name'
]
```

## 📂 File Structure

```
vue-weather-app/
├── index.html              # Main application file
├── README.md               # Project documentation
└── assets/                 # (Optional) Additional assets
    ├── screenshots/        # App screenshots
    └── icons/             # Custom icons
```

## 🌐 Browser Support

- **Chrome**: 88+
- **Firefox**: 85+
- **Safari**: 14+
- **Edge**: 88+

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 🔮 Future Enhancements

- [ ] Integration with real weather APIs (OpenWeatherMap, WeatherAPI)
- [ ] Geolocation support for automatic location detection
- [ ] Weather alerts and notifications
- [ ] Hourly forecast view
- [ ] Weather maps integration
- [ ] Dark mode theme
- [ ] Export weather data functionality
- [ ] PWA (Progressive Web App) support

## 📞 Support

If you have any questions or run into issues, please:

1. Check the existing [Issues](https://github.com/yourusername/vue-weather-app/issues)
2. Create a new issue if your problem isn't already reported
3. Provide detailed information about your browser and operating system

## 🙏 Acknowledgments

- Vue.js team for the excellent framework
- Vuetify team for the Material Design components
- TailwindCSS for utility-first CSS framework
- Font Awesome for the beautiful icons
- OpenWeatherMap for weather data inspiration

---

**Built with ❤️ using Vue.js, Vuetify, and TailwindCSS**
