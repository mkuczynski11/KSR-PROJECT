const axios = require("axios").default;
const API_URL = "http://localhost/api";
const BOOKS_API_URL = "http://localhost:8080/api";


export async function getBooks() {
  console.log(`Fetching books from ${BOOKS_API_URL}`);
  let res = await axios
  .get(`${BOOKS_API_URL}/warehouse/books`, {
      params: {},
    })
    .then((response) => {
      return response;
    });
    
    return res.data;
  }
  
  export async function getBook(id) {
    console.log(`Fetching one book from ${BOOKS_API_URL}`);
    let res = await axios
    .get(`${BOOKS_API_URL}/warehouse/books/${id}`, {
      params: {},
    })
    .then((response) => {
      return response;
    });
    
    return res.data;
  }
  
  // export async function getUsers() {
  //   console.log(`Fetching users from ${API_URL}`);
  //   let res = await axios
  //     .get(`${API_URL}/users`, {
  //       params: {},
  //     })
  //     .then((response) => {
  //       return response;
  //     });
  
  //   return res.data;
  // }

// export async function getUser(username) {
//   console.log(`Fetching one user from ${API_URL}`);
//   let res = await axios
//     .get(`${API_URL}/users/${username}`, {
//       params: {},
//     })
//     .then((response) => {
//       return response;
//     });

//   return res.data;
// }

export async function getUserCars(user) {
  console.log(`Fetching user cars from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/users/${user}/cars`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function deleteUser(user) {
  let res = await axios
    .delete(`${API_URL}/users/${user}`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function createUser(user) {
  let res = await axios
    .post(
      `${API_URL}/users`,
      {
        login: user.login,
        name: user.name,
        surname: user.surname,
        email: user.email,
        password: user.password,
        birthdate: user.birthdate,
      },
      {
        headers: {
          "Content-Type": "application/json",
        },
      }
    )
    .then((response) => {
      return response;
    });

  return res;
}

export async function editUser(user) {
  let res = await axios
    .put(
      `${API_URL}/users/${user.login}`,
      {
        name: user.name,
        surname: user.surname,
        email: user.email,
        password: user.password,
        birthdate: user.birthdate,
      },
      {
        headers: {
          "Content-Type": "application/json",
        },
      }
    )
    .then((response) => {
      return response;
    });

  return res;
}

export async function getCars() {
  
  console.log(`Fetching cars from ${API_URL}`);
  let res = await axios
  .get(`${API_URL}/cars`, {
    params: {},
  })
  .then(response => {
    return response;
  });
  
  return res.data;
}

export async function getCar(id) {
  
  console.log(`Fetching cars from ${API_URL}`);
  let res = await axios
  .get(`${API_URL}/cars/${id}`, {
    params: {},
  })
  
  .then(response => {
    return response;
  });
  
  return res.data;
}

export async function deleteCar(car) {
  let res = await axios
  .delete(`${API_URL}/cars/${car}`, {
    params: {},
  })
  .then((response) => {
    return response;
  });
  
  return res.data;
}

export async function createCar(car) {
  console.log(car)
  let res = await axios
  .post(
    `${API_URL}/cars`,
    {
      id: car.id,
      name: car.name,
      maxSpeed: car.maxSpeed,
      horsePower: car.horsePower,
      displacement: car.displacement,
      seats: car.seats,
      doors: car.doors,
      wheels: car.wheels,
      user: car.user,
    },
    {
      headers: {
        "Content-Type": "application/json",
      },
    }
    )
    .then((response) => {
      return response;
    });
    
    return res;
  }
  
  export async function editCar(car) {
    let res = await axios
      .put(
        `${API_URL}/cars/${car.id}`,
        {
          maxSpeed: car.maxSpeed,
          horsePower: car.horsePower,
          seats: car.seats
        },
        {
          headers: {
            "Content-Type": "application/json",
          },
        }
      )
      .then((response) => {
        return response;
      });
  
    return res;
  }
  
  const api = {
  getBooks,
  getBook,
  getUserCars,
  deleteUser,
};

export default api;
