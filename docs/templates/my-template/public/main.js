// Google Analytics only on the published site, so that docfx serve (localhost) and the headless
// browser of docfx pdf (127.0.0.1) are not counted as visitors
const gaTagId = 'G-1MMW827H44'
if (location.hostname === 've3nea.github.io') {
  const script = document.createElement('script')
  script.async = true
  script.src = `https://www.googletagmanager.com/gtag/js?id=${gaTagId}`
  document.head.appendChild(script)
  window.dataLayer = window.dataLayer || []
  window.gtag = function () { window.dataLayer.push(arguments) }
  window.gtag('js', new Date())
  window.gtag('config', gaTagId)
}

export default {}
