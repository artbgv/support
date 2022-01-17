import React from "react";

export default function App() {
  function doRequest() {
    const url = 'https://localhost:7074/api/Test';

    fetch(url, {
      method: 'GET'
    })
    .then(response => response.json())
    .then(data => {
      console.log(data);
    })
    .catch((error) => {
      console.log(error);
      alert(error);
    });
  }

  return (
    <>
      <h1>Support System</h1>
      <button onClick={doRequest}>Click</button>
    </>
  );
}
